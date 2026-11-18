# 停止恢复回检优先 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 人工停止后恢复运行时，先完成已放料模组的上相机二次回检，未完成前禁止下一批取料或放料。

**Architecture:** 保持单一待回检批次，不实现多批排队。`FeedbackBatch` 提供不可覆盖的“尝试开始”接口；`UpDownLoad` 在状态机计算下一动作前执行恢复回检闸门，并在两条放料入口增加防御性拦截，防止任何调用路径覆盖旧批次的模穴或坐标。

**Tech Stack:** C# .NET Framework WinForms；PowerShell 反射/源码契约测试；MSBuild。

---

### Task 1: 为单一待回检批次建立不可覆盖契约

**Files:**
- Modify: `UI/Class/FeedbackBatch.cs:10-43`
- Modify: `tests/FeedbackBatch.Contract.ps1:1-39`

- [ ] **Step 1: 写入失败的批次覆盖回归测试**

在 `tests/FeedbackBatch.Contract.ps1` 的最后一次 `Complete` 断言之前加入：

```powershell
$blockedArgs = New-Object object[] 1
$blockedArgs[0] = [int[]](2, 6)
$wasStarted = $type.GetMethod("TryBegin").Invoke($batch, $blockedArgs)
if ($wasStarted) { throw "A pending feedback batch must block a replacement batch." }
if (-not $type.GetMethod("IsPending").Invoke($batch, @(5))) {
    throw "The original pending module must remain pending when a later batch is rejected."
}
```

- [ ] **Step 2: 运行测试，确认因缺少接口失败**

Run: `powershell -ExecutionPolicy Bypass -File tests/FeedbackBatch.Contract.ps1`

Expected: FAIL，错误包含 `TryBegin` 或空方法调用。

- [ ] **Step 3: 实现最小的不可覆盖接口**

在 `UI/Class/FeedbackBatch.cs` 的 `Begin` 前新增：

```csharp
public bool TryBegin(IEnumerable<int> moduleNumbers)
{
    if (HasPending) return false;
    Begin(moduleNumbers);
    return true;
}
```

- [ ] **Step 4: 运行批次测试，确认通过**

Run: `powershell -ExecutionPolicy Bypass -File tests/FeedbackBatch.Contract.ps1`

Expected: `FeedbackBatch contract passed.`

### Task 2: 将恢复回检设为状态机最高优先级

**Files:**
- Create: `tests/FeedbackRecoveryGate.Contract.ps1`
- Modify: `UI/Class/UpDownLoad.cs:133-155`
- Modify: `UI/Class/UpDownLoad.cs:4600-4660`

- [ ] **Step 1: 写入失败的恢复闸门源码契约测试**

创建 `tests/FeedbackRecoveryGate.Contract.ps1`：

```powershell
$sourcePath = Join-Path $PSScriptRoot "..\UI\Class\UpDownLoad.cs"
$source = Get-Content -LiteralPath $sourcePath -Raw
$gate = $source.IndexOf("RecoverPendingFeedbackBeforeNextAction(ref bquit, wsfeed)")
$nextAction = $source.IndexOf("Proc = ChkNextProc", [System.StringComparison]::Ordinal)
if ($gate -lt 0) { throw "Recovery feedback gate is required." }
if ($nextAction -lt 0 -or $gate -gt $nextAction) {
    throw "Pending feedback must be recovered before selecting the next pick/place action."
}
if ($source -notmatch "if \(feedbackBatch\.HasPending\)") {
    throw "Recovery feedback gate must only run for unfinished feedback."
}
Write-Host "Feedback recovery gate contract passed."
```

- [ ] **Step 2: 运行测试，确认闸门尚不存在**

Run: `powershell -ExecutionPolicy Bypass -File tests/FeedbackRecoveryGate.Contract.ps1`

Expected: FAIL，错误包含 `Recovery feedback gate is required.`

- [ ] **Step 3: 实现恢复回检辅助方法和状态机闸门**

在 `CompleteFeedbackModule` 后新增：

```csharp
private EM_RES RecoverPendingFeedbackBeforeNextAction(ref bool bquit, WSFEED ws)
{
    if (!feedbackBatch.HasPending) return EM_RES.OK;

    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR,
        string.Format("{0}检测到停止前未完成的工站回检，优先回检后再继续取放料", disc));
    ST_XYZA endpos = new ST_XYZA();
    EM_RES res = FlyCamToLastPos(ref bquit, ws, ref endpos);
    if (res != EM_RES.OK && res != EM_RES.NEXT && res != EM_RES.GIVEWAY) return res;
    return feedbackBatch.HasPending ? EM_RES.RETRY : EM_RES.OK;
}
```

在 `UpDownLoadModleAct` 的 `Proc == EM_STA.CHECK` 分支、`Proc = ChkNextProc(...)` 之前加入：

```csharp
if (feedbackBatch.HasPending)
{
    res = RecoverPendingFeedbackBeforeNextAction(ref bquit, wsfeed);
    if (res != EM_RES.OK) return res;
    Proc = EM_STA.CHECK;
    continue;
}
```

- [ ] **Step 4: 运行恢复闸门测试，确认通过**

Run: `powershell -ExecutionPolicy Bypass -File tests/FeedbackRecoveryGate.Contract.ps1`

Expected: `Feedback recovery gate contract passed.`

### Task 3: 防止旁路覆盖并验证工程

**Files:**
- Modify: `UI/Class/UpDownLoad.cs:3485-3565`
- Modify: `tests/FeedbackRecoveryGate.Contract.ps1`

- [ ] **Step 1: 扩充失败的双放料入口防御测试**

在 `tests/FeedbackRecoveryGate.Contract.ps1` 的 `Write-Host` 前加入：

```powershell
$guardedCalls = [regex]::Matches($source, "if \(!BeginFeedbackBatch\(WsTriPos\)\) return EM_RES\.RETRY;")
if ($guardedCalls.Count -ne 2) {
    throw "Both FlyToWs entry points must reject a new placement while feedback is pending."
}
```

- [ ] **Step 2: 运行测试，确认两个入口尚未受保护**

Run: `powershell -ExecutionPolicy Bypass -File tests/FeedbackRecoveryGate.Contract.ps1`

Expected: FAIL，错误包含 `Both FlyToWs entry points`。

- [ ] **Step 3: 令放料批次开始操作返回结果，并保护两条放料入口**

将 `BeginFeedbackBatch` 改为：

```csharp
private bool BeginFeedbackBatch(List<WSFEED.MdDat> modules)
{
    if (!feedbackBatch.TryBegin(modules.Select(module => module.Num))) return false;
    FeedBackWs.Clear();
    foreach (WSFEED.MdDat module in modules)
        FeedBackWs.Add(module.Clone());
    return true;
}
```

分别在 `FlyToWs` 与 `FlyToWsaction` 中把：

```csharp
BeginFeedbackBatch(WsTriPos);
```

替换为：

```csharp
if (!BeginFeedbackBatch(WsTriPos)) return EM_RES.RETRY;
```

此返回发生在 `pos_flystop`（即 `FeedBackPos`）被赋值之前，确保任何旁路也无法用 2、6 的坐标覆盖 1、5。

- [ ] **Step 4: 运行全部契约测试和编译**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tests/FeedbackBatch.Contract.ps1
powershell -ExecutionPolicy Bypass -File tests/FeedbackRecoveryGate.Contract.ps1
msbuild Pre-Mini.sln /t:Build /p:Configuration=Debug /p:Platform=x64
```

Expected: 两个契约测试均输出 `passed`，MSBuild exit code 为 0。
