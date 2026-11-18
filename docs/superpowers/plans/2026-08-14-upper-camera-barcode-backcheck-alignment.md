# 上相机条码回检与下相机对齐 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 使上相机条码二次回检在读空和读码不一致时，采用与下相机一致的重拍、报警、标记 `111` 和批次收尾行为。

**Architecture:** 新增一个纯条码判定类，仅负责区分匹配、读空和不一致，供上相机回检流程调用。`UpDownLoad` 中新增一个共享的上相机单点重拍/弹窗处理方法，普通回检和最后位置回检都调用该方法，并保留现有 `wsBarcodeErr` 的批次延后返回机制。

**Tech Stack:** C# .NET Framework 4.8、WinForms、PowerShell 反射契约测试、MSBuild。

---

## File Structure

- Create: `UI/Class/BarcodeBackcheckPolicy.cs` — 纯函数，分类条码匹配、读空和不一致。
- Create: `tests/BarcodeBackcheckPolicy.Contract.ps1` — 加载编译后的 UI 程序集，验证分类契约。
- Modify: `UI/UI.csproj` — 编译新的策略类。
- Modify: `UI/Class/UpDownLoad.cs` — 添加上相机单点重拍/弹窗处理，并接入普通与最后位置回检循环。

### Task 1: 条码判定契约

**Files:**
- Create: `tests/BarcodeBackcheckPolicy.Contract.ps1`
- Create: `UI/Class/BarcodeBackcheckPolicy.cs`
- Modify: `UI/UI.csproj:320-325`

- [ ] **Step 1: 写入失败的契约测试**

```powershell
$type = $assembly.GetType("UI.BarcodeBackcheckPolicy", $false)
if ($null -eq $type) { throw "BarcodeBackcheckPolicy type is required." }

$classify = $type.GetMethod("Classify")
if ($classify.Invoke($null, @("ABC", "ABC")).ToString() -ne "Match") { throw "Matching barcode must be Match." }
if ($classify.Invoke($null, @("ABC", "")).ToString() -ne "Empty") { throw "Empty actual barcode must be Empty." }
if ($classify.Invoke($null, @("ABC", "XYZ")).ToString() -ne "Mismatch") { throw "Different non-empty barcode must be Mismatch." }
if ($classify.Invoke($null, @("", "XYZ")).ToString() -ne "Mismatch") { throw "Missing recorded barcode with a scanned value must be Mismatch." }
```

- [ ] **Step 2: 运行测试并确认失败**

Run:

```powershell
& '.\tests\BarcodeBackcheckPolicy.Contract.ps1'
```

Expected: `BarcodeBackcheckPolicy type is required.`

- [ ] **Step 3: 实现最小条码判定类并加入项目**

```csharp
namespace UI
{
    public enum BarcodeBackcheckResult
    {
        Match,
        Empty,
        Mismatch
    }

    public static class BarcodeBackcheckPolicy
    {
        public static BarcodeBackcheckResult Classify(string expectedBarcode, string actualBarcode)
        {
            if (string.IsNullOrWhiteSpace(actualBarcode)) return BarcodeBackcheckResult.Empty;
            if (actualBarcode == expectedBarcode) return BarcodeBackcheckResult.Match;
            return BarcodeBackcheckResult.Mismatch;
        }
    }
}
```

Add the following compile entry next to the existing `Class\\DataBase.cs` entry:

```xml
<Compile Include="Class\BarcodeBackcheckPolicy.cs" />
```

- [ ] **Step 4: 编译并确认契约测试通过**

Run:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' 'Pre-Mini.sln' /t:Build /p:Configuration=Debug /p:Platform='Any CPU' /m /v:minimal
& '.\tests\BarcodeBackcheckPolicy.Contract.ps1'
```

Expected: MSBuild exits `0`; the test prints `BarcodeBackcheckPolicy contract passed.`

- [ ] **Step 5: 提交策略与契约测试**

```powershell
git add -- 'UI/Class/BarcodeBackcheckPolicy.cs' 'UI/UI.csproj' 'tests/BarcodeBackcheckPolicy.Contract.ps1'
git commit -m "feat: classify barcode backcheck results"
```

### Task 2: 上相机单点重拍与读空弹窗

**Files:**
- Modify: `UI/Class/UpDownLoad.cs:146-155`
- Modify: `UI/Class/UpDownLoad.cs:1752-1766`

- [ ] **Step 1: 为共享处理器写入失败的集成契约**

Extend `tests/BarcodeBackcheckPolicy.Contract.ps1` with this reflection check:

```powershell
$upDownLoadType = $assembly.GetType("UI.UpDownLoad", $false)
$handler = $upDownLoadType.GetMethod("VerifyUpperBarcodeBackcheck", [System.Reflection.BindingFlags]"Instance, NonPublic")
if ($null -eq $handler) { throw "VerifyUpperBarcodeBackcheck is required for upper-camera retry handling." }
```

- [ ] **Step 2: 运行测试并确认失败**

Run:

```powershell
& '.\tests\BarcodeBackcheckPolicy.Contract.ps1'
```

Expected: `VerifyUpperBarcodeBackcheck is required for upper-camera retry handling.`

- [ ] **Step 3: 在 `UpDownLoad` 中实现共享处理器**

Add this method near `CompleteFeedbackModule`:

```csharp
private EM_RES VerifyUpperBarcodeBackcheck(
    ref bool bquit,
    WSFEED ws,
    ST_XYN position,
    string expectedBarcode,
    string actualBarcode,
    ref bool barcodeErr,
    string flowName)
{
    BarcodeBackcheckResult result = BarcodeBackcheckPolicy.Classify(expectedBarcode, actualBarcode);
    if (result == BarcodeBackcheckResult.Match) return EM_RES.OK;

RECAP:
    EM_RES res = MT.ZupMove(ref bquit, ref ax_x, position.x, ref ax_y, position.y);
    if (res != EM_RES.OK) return res;
    res = upcam.FindTaskTriAndWait(CONST.WsModUpFw, Demo);
    if (res != EM_RES.OK && res != EM_RES.CAM_ERR) return res;

    result = BarcodeBackcheckPolicy.Classify(expectedBarcode, upcam.curTask.ResData.BarCode);
    if (result == BarcodeBackcheckResult.Empty)
    {
        COUNT_DATA.ErrTemp = System.DateTime.Now;
        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "没检到二维码!" : "No Barcode", 20, true);
        MT.ST_WARN warn = new MT.ST_WARN();
        warning dialog = new warning();
        warn.ok_txt = VAR.IsChinese ? (PT_SET.bBackerrcontinue ? "继续运行" : "重新拍照")
            : (PT_SET.bBackerrcontinue ? "Keep running" : "Take a photo");
        if (PT_SET.bBackerrcontinue) warn.abort_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
        warn.ws = null;
        warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
        warn.title = VAR.IsChinese ? "提示:没检到二维码" : "Tip: No barcode detected";
        warn.msg = VAR.IsChinese ? string.Format("上相机{0}上模组二维码无法识别.", ws.disc)
            : string.Format("Upcam {0} module barcode cannot be identified", ws.disc);
        warn.lb_msg = VAR.IsChinese ? "提示:" + warn.msg + "请确认!\\r\\n  1.按'继续运行'键则放弃当前检查继续运行!\\r\\n  2.如按重新拍照键则重新获取当前位置图像!"
            : "Tip:" + warn.msg + "\\r\\nPlease confirm!";
        SQLData.TesterrDataAdd(99, 99, "料盘放料", string.Format("上下料{0}未检测到二维码", id));
        COUNT_DATA.Errcnt++;
        DialogResult choice = MT.Display_frwarn(dialog, warn, ERR_ALM.EmErrItem.BarcodeAbnormal);
        if (choice == DialogResult.Cancel) return EM_RES.ERR;
        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
        MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
        if (PT_SET.bBackerrcontinue || choice == DialogResult.OK) goto RECAP;
        return EM_RES.ERR;
    }

    if (result == BarcodeBackcheckResult.Mismatch)
    {
        WSFEED.MdDat md = ws.list_md[position.n];
        md.res = 111;
        ws.breschanged = true;
        barcodeErr = true;
        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR,
            string.Format("上相机{0}二维码回检失败,工站:{1},位置:{2},记录:{3},实拍:{4}",
                flowName, ws.disc, md.Num, md.bardcode ?? "<空>", upcam.curTask.ResData.BarCode ?? "<空>"));
    }
    return EM_RES.OK;
}
```

The `Empty` branch above intentionally preserves the lower-camera semantics: when `bBackerrcontinue` is true, both `继续运行` and `重新拍照` return to `RECAP`; otherwise the single non-stop action returns to `RECAP`. `停止运行` returns `EM_RES.ERR`.

Replace the ordinary upper-camera inline comparison with:

```csharp
res = VerifyUpperBarcodeBackcheck(ref bquit, ws, WsTriPos[i], md.bardcode,
    task.ResData.BarCode, ref wsBarcodeErr, "普通飞拍");
if (res != EM_RES.OK) return res;
```

- [ ] **Step 4: 编译并确认两个契约测试通过**

Run:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' 'Pre-Mini.sln' /t:Build /p:Configuration=Debug /p:Platform='Any CPU' /m /v:minimal
& '.\tests\FeedbackBatch.Contract.ps1'
& '.\tests\BarcodeBackcheckPolicy.Contract.ps1'
```

Expected: MSBuild exits `0`; both scripts print `contract passed.`

- [ ] **Step 5: 提交上相机普通回检处理**

```powershell
git add -- 'UI/Class/UpDownLoad.cs' 'tests/BarcodeBackcheckPolicy.Contract.ps1'
git commit -m "feat: align upper barcode retry handling"
```

### Task 3: 最后位置回检复用共享处理器

**Files:**
- Modify: `UI/Class/UpDownLoad.cs:2178-2192`
- Test: `tests/BarcodeBackcheckPolicy.Contract.ps1`

- [ ] **Step 1: 写入失败的源代码结构契约**

Append to the PowerShell contract test:

```powershell
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\UI\Class\UpDownLoad.cs") -Raw
$lastPositionSection = $source.Substring($source.IndexOf("public EM_RES FlyCamToLastPos"))
if ($lastPositionSection -notmatch "VerifyUpperBarcodeBackcheck\(ref bquit, ws, WsTriPos\[i\]") {
    throw "Last-position feedback must reuse VerifyUpperBarcodeBackcheck."
}
```

- [ ] **Step 2: 运行测试并确认失败**

Run:

```powershell
& '.\tests\BarcodeBackcheckPolicy.Contract.ps1'
```

Expected: `Last-position feedback must reuse VerifyUpperBarcodeBackcheck.`

- [ ] **Step 3: 替换最后位置路径中的内联条码判断**

Replace the `FlyCamToLastPos` upper-camera barcode comparison with:

```csharp
res = VerifyUpperBarcodeBackcheck(ref bquit, ws, WsTriPos[i], md.bardcode,
    task.ResData.BarCode, ref wsBarcodeErr, "最后位置");
if (res != EM_RES.OK) return res;
```

Keep `CompleteFeedbackModule(WsTriPos[i].n + 1)` after all checks for the current module, and keep the existing `if (wsBarcodeErr) return EM_RES.RETRY;` after the loop.

- [ ] **Step 4: 完整验证**

Run:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' 'Pre-Mini.sln' /t:Build /p:Configuration=Debug /p:Platform='Any CPU' /m /v:minimal
& '.\tests\FeedbackBatch.Contract.ps1'
& '.\tests\BarcodeBackcheckPolicy.Contract.ps1'
git -c core.whitespace=cr-at-eol diff --check
```

Expected: all commands exit `0`.

- [ ] **Step 5: 提交最后位置回检复用**

```powershell
git add -- 'UI/Class/UpDownLoad.cs' 'tests/BarcodeBackcheckPolicy.Contract.ps1'
git commit -m "fix: reuse upper barcode retry handling"
```

### Task 4: 机台验证

**Files:**
- Inspect: `UI/Class/UpDownLoad.cs:1752-1766`
- Inspect: `UI/Class/UpDownLoad.cs:2178-2192`

- [ ] **Step 1: 普通回检不一致验证**

Place two modules in one feedback batch. Make the first module's upper-camera barcode differ after the single-point retry. Confirm the first result becomes `111`, the second module is still checked, and the run reports `RETRY` only after the second feedback result is logged.

- [ ] **Step 2: 读空弹窗验证**

Block the upper-camera barcode read. Confirm the dialog controls reflect `bBackerrcontinue`; choose each enabled non-stop choice and confirm the machine returns to the same module's upper-camera capture position. Choose stop and confirm movement ends without a further camera trigger.

- [ ] **Step 3: NG 下料验证**

After a module becomes `111`, restart the machine if the batch returns `RETRY`. Confirm the module is picked and placed in the NG tray; verify the log includes `料盘放料开始，模组结果111` followed by the suction-head placement-complete log.
