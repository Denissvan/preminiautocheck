# Pre-Mini 二次回检停止恢复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 停止后保留未完成二次回检模穴，恢复后逐颗补检；首颗二维码异常不跳过同组其他模组。

**Architecture:** 在 `UpDownLoad` 中增加一个仅负责模穴待办状态的 `FeedbackBatch`。`FeedBackWs` 继续保存放料快照，批次对象保存未完成模穴号。回检入口仅根据待办项执行，二维码异常在循环结束后统一返回 `RETRY`。

**Tech Stack:** C#、.NET Framework 4.8、WinForms、现有 UI 项目。

---

### Task 1: 添加可测试的回检待办状态

**Files:**
- Create: `UI/Class/FeedbackBatch.cs`
- Create: `UI.Tests/UI.Tests.csproj`
- Create: `UI.Tests/FeedbackBatchTests.cs`
- Modify: `Pre-Mini.sln`

- [ ] **Step 1: 写入失败测试**

```csharp
[TestMethod]
public void StopAfterOneModule_KeepsOnlyUnfinishedModulePending()
{
    var batch = new FeedbackBatch();
    batch.Begin(new[] { 1, 5 });
    batch.MarkCompleted(1);
    CollectionAssert.AreEqual(new[] { 5 }, batch.PendingNumbers.ToArray());
}

[TestMethod]
public void FinishedBatch_AllowsNextBatch()
{
    var batch = new FeedbackBatch();
    batch.Begin(new[] { 1, 5 });
    batch.MarkCompleted(1);
    batch.MarkCompleted(5);
    Assert.IsTrue(batch.CanBeginNextBatch);
}
```

- [ ] **Step 2: 运行并确认失败**

Run: `msbuild Pre-Mini.sln /t:UI.Tests /p:Configuration=Debug`

Expected: 失败，提示 `FeedbackBatch` 类型不存在。

- [ ] **Step 3: 实现状态模型**

```csharp
public sealed class FeedbackBatch
{
    private readonly List<int> pendingNumbers = new List<int>();
    public IReadOnlyList<int> PendingNumbers => pendingNumbers;
    public bool HasPending => pendingNumbers.Count > 0;
    public bool CanBeginNextBatch => !HasPending;

    public void Begin(IEnumerable<int> numbers)
    {
        if (HasPending) throw new InvalidOperationException("当前二次回检尚未完成。");
        pendingNumbers.Clear();
        pendingNumbers.AddRange(numbers.Distinct());
    }

    public void MarkCompleted(int number) => pendingNumbers.Remove(number);
    public void Clear() => pendingNumbers.Clear();
}
```

- [ ] **Step 4: 重建并运行测试**

Run: `msbuild Pre-Mini.sln /t:Build /p:Configuration=Debug`

Run: `vstest.console.exe UI.Tests\\bin\\Debug\\UI.Tests.dll`

Expected: 两个测试通过。

- [ ] **Step 5: 提交状态模型**

```powershell
git add UI/Class/FeedbackBatch.cs UI.Tests Pre-Mini.sln
git commit -m "test: add feedback batch state coverage"
```

### Task 2: 放料时创建待办，回检时只处理未完成模穴

**Files:**
- Modify: `UI/Class/UpDownLoad.cs:118-124, 1505-1560, 2037-2080, 3387-3398, 3448-3459, 3680-3682`
- Test: `UI.Tests/FeedbackBatchTests.cs`

- [ ] **Step 1: 增加失败测试，验证完成项不会在恢复后重复出现**

```csharp
[TestMethod]
public void CompletedModule_IsNotPendingAfterResume()
{
    var batch = new FeedbackBatch();
    batch.Begin(new[] { 1, 5 });
    batch.MarkCompleted(5);
    CollectionAssert.AreEqual(new[] { 1 }, batch.PendingNumbers.ToArray());
}
```

- [ ] **Step 2: 修改 `UpDownLoad` 的回检状态接入**

```csharp
private readonly FeedbackBatch feedbackBatch = new FeedbackBatch();

private List<WSFEED.MdDat> GetPendingFeedbackModules()
{
    return FeedBackWs.Where(x => feedbackBatch.PendingNumbers.Contains(x.Num)).ToList();
}

private void BeginFeedbackBatch(IEnumerable<WSFEED.MdDat> modules)
{
    if (feedbackBatch.HasPending) return;
    FeedBackWs.Clear();
    FeedBackWs.AddRange(modules.Select(x => x.Clone()));
    feedbackBatch.Begin(FeedBackWs.Select(x => x.Num));
}
```

将两个 `FlyToWs` 方法中对 `FeedBackWs` 的直接清空/填充替换为 `BeginFeedbackBatch(WsTriPos)`。将 `FlyCamToTray` 和 `FlyCamToLastPos` 的回检入口改为只使用 `GetPendingFeedbackModules()`，不再以 `bUpdateFBPos_*` 为门槛。

- [ ] **Step 3: 在每颗模组判定成功完成后更新待办**

```csharp
if (bquit)
{
    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
        string.Format("{0}二次回检取消，待回检模穴:{1}", disc,
        string.Join(",", feedbackBatch.PendingNumbers)));
    return EM_RES.QUIT;
}

feedbackBatch.MarkCompleted(md.Num);
VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
    string.Format("{0}模穴{1}二次回检完成", disc, md.Num));
if (!feedbackBatch.HasPending) FeedBackWs.Clear();
```

运动、相机等待或报警选择停止返回时不得调用 `MarkCompleted`。

- [ ] **Step 4: 执行测试与编译**

Run: `vstest.console.exe UI.Tests\\bin\\Debug\\UI.Tests.dll`

Run: `msbuild Pre-Mini.sln /t:Build /p:Configuration=Debug`

Expected: 三个状态模型测试通过，所有项目编译成功。

### Task 3: 聚合二维码异常，修正停止日志与异步标志干扰

**Files:**
- Modify: `UI/Class/UpDownLoad.cs:1538-1541, 1723-1735, 2136-2148, 2788-2808`
- Test: `UI.Tests/FeedbackBatchTests.cs`

- [ ] **Step 1: 写入同组异常测试**

```csharp
[TestMethod]
public void FailedFirstModule_LeavesSecondModulePendingUntilProcessed()
{
    var batch = new FeedbackBatch();
    batch.Begin(new[] { 1, 5 });
    batch.MarkCompleted(1);
    CollectionAssert.AreEqual(new[] { 5 }, batch.PendingNumbers.ToArray());
}
```

- [ ] **Step 2: 将二维码不一致改为批次聚合，而不是循环内退出**

```csharp
bool barcodeFeedbackFailed = false;
if (barcodeMismatch)
{
    md.res = 111;
    ws.breschanged = true;
    barcodeFeedbackFailed = true;
    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR,
        string.Format("上相机二次回检二维码失败,工站:{0},位置:{1},记录:{2},实拍:{3}",
            ws.disc, md.Num, md.bardcode ?? "<空>", task.ResData.BarCode ?? "<空>"));
}
feedbackBatch.MarkCompleted(md.Num);

// 同批所有相机结果已处理后：
if (!feedbackBatch.HasPending && barcodeFeedbackFailed)
    return EM_RES.RETRY;
```

在普通飞拍和最后位置回检的二维码分支均采用该模式。两处循环不得在第一颗二维码异常时 `return RETRY`。

- [ ] **Step 3: 修正回拍定位日志与异步标志写入**

```csharp
res = MT.ZupMove(ref bquit, ref ax_x, FeedBackPos.x, ref ax_y, FeedBackPos.y,
    ref ax_u1, PT_SET.isopen_degree ? -PT_SET.degree : 0,
    ref ax_u2, PT_SET.isopen_degree ? -PT_SET.degree : 0);
if (res != EM_RES.OK)
{
    if (res == EM_RES.QUIT || bquit)
        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}二次回检定位取消", disc));
    return res;
}
VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
    string.Format("{0}料盘放料定位到回拍: X:{1} Y:{2}", disc, FeedBackPos.x, FeedBackPos.y));
```

移除 `MoveNextTrayX1Pos` 中对 `bUpdateFBPos_OKNG` 和 `bUpdateFBPos_UT` 的清除；料盘提前定位不得改变二次回检待办。

- [ ] **Step 4: 编译和现场回归**

Run: `msbuild Pre-Mini.sln /t:Build /p:Configuration=Debug`

现场验证：

1. 1 回检完成、5 未开始时停止；重启后仅补检 5。
2. 回拍定位中停止；本轮没有相机触发，重启后 1、5 均补检。
3. 1 二维码异常、5 正常；两颗均有回检日志，1 为 111。
4. 正常跑完 1、5、2、6、3、7、4、8；每颗仅一次“二次回检完成”。

- [ ] **Step 5: 提交最终修复**

```powershell
git add UI/Class/UpDownLoad.cs UI/Class/FeedbackBatch.cs UI.Tests
git commit -m "fix: resume per-module feedback after stop"
```
