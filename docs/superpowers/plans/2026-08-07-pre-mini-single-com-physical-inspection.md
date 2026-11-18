# Pre-Mini Single COM Physical Inspection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace four point-inspection serial-port settings per workstation with one integrated COM that measures distance, Lux, and CCT.

**Architecture:** The existing product INI stores one integrated point-inspection COM per workstation. One SensorReader stays open while a test position completes distance measurement followed by one Lux/CCT measurement at each of Z1, Z2, and Z3. PreMiniAutoChecker owns retries; WSFEED schedules and stops on failure.

**Tech Stack:** C#/.NET Framework 4.8, WinForms, System.IO.Ports, Newtonsoft.Json, IniFile, MSBuild.

---

## Files

- Modify: MotionCtrl/Class/MyType.cs lines 1286-1303, 1450-1467, 1613-1630 — two COM fields and INI read/write.
- Modify: UI/FormView/FrSys.Designer.cs lines 4201-4900 — two COM controls and two manual-read buttons.
- Modify: UI/FormView/FrSys.cs lines 584-606, 782-802, 870-975 — settings bindings, validation, manual read.
- Modify: UI/Class/PreMiniAutoChecker.cs lines 14-336 — single-session physical inspection.
- Modify: UI/Class/WSFEED.cs lines 2682-2714 — remove the skip path.
- Verify: Pre-Mini.sln — x64 Debug rebuild and bench test.

No unit-test project exists. Serial-port and motion behavior must be verified with the bench matrix in Task 4 after the full solution build succeeds.

### Task 1: Replace the configuration model

**Files:**

- Modify: MotionCtrl/Class/MyType.cs lines 1286-1303, 1450-1467, 1613-1630

- [ ] **Step 1: Confirm the legacy declarations, reads, and writes exist**

Run:

~~~powershell
rg -n 'PreMiniWs[12](DistanceCom|LuxCctCom[123])' MotionCtrl/Class/MyType.cs
~~~

Expected: eight legacy COM fields, reads, and writes.

- [ ] **Step 2: Replace the eight static COM fields**

Replace the eight declarations with:

~~~csharp
public static string PreMiniWs1AutoCheckCom = "";
public static string PreMiniWs2AutoCheckCom = "";
~~~

Keep enable flags, time settings, start position, and all Z1/Z2/Z3 settings unchanged.

- [ ] **Step 3: Read and write exactly two new INI keys**

Replace all legacy COM reads and writes with:

~~~csharp
PreMiniWs1AutoCheckCom = inf.ReadString("AutoCheckParam", "PreMiniWs1AutoCheckCom", "");
PreMiniWs2AutoCheckCom = inf.ReadString("AutoCheckParam", "PreMiniWs2AutoCheckCom", "");

inf.WriteString("AutoCheckParam", "PreMiniWs1AutoCheckCom", PreMiniWs1AutoCheckCom, ref ischange, true, filename);
inf.WriteString("AutoCheckParam", "PreMiniWs2AutoCheckCom", PreMiniWs2AutoCheckCom, ref ischange, true, filename);
~~~

Do not migrate or fall back to old keys: the new physical wiring must be selected explicitly.

- [ ] **Step 4: Verify the replacement and commit**

Run:

~~~powershell
rg -n 'PreMiniWs[12](DistanceCom|LuxCctCom[123])' MotionCtrl/Class/MyType.cs
rg -n 'PreMiniWs[12]AutoCheckCom' MotionCtrl/Class/MyType.cs
git add MotionCtrl/Class/MyType.cs
git commit -m "refactor: use one pre-mini inspection COM per station"
~~~

Expected: the first search has no matches; the second finds two declarations, two reads, and two writes; only MyType.cs is committed.

### Task 2: Reduce the settings page to two integrated COM controls

**Files:**

- Modify: UI/FormView/FrSys.Designer.cs lines 4201-4900
- Modify: UI/FormView/FrSys.cs lines 584-606, 782-802, 870-975

- [ ] **Step 1: Replace the designer controls**

Replace the eight legacy COM ComboBoxes, labels, and manual-read buttons with:

~~~csharp
private System.Windows.Forms.ComboBox cmbPreMiniWs1AutoCheckCom;
private System.Windows.Forms.ComboBox cmbPreMiniWs2AutoCheckCom;
private System.Windows.Forms.Button btnPreMiniWs1ReadAutoCheck;
private System.Windows.Forms.Button btnPreMiniWs2ReadAutoCheck;
~~~

Label them 右工站综合点检COM and 左工站综合点检COM. Bind the two buttons to btnPreMiniWs1ReadAutoCheck_Click and btnPreMiniWs2ReadAutoCheck_Click. Preserve enable switches, start position, times, and all Z text boxes.

- [ ] **Step 2: Load and save the two values**

Replace the legacy bindings with:

~~~csharp
cmbPreMiniWs1AutoCheckCom.Text = PT_SET.PreMiniWs1AutoCheckCom;
cmbPreMiniWs2AutoCheckCom.Text = PT_SET.PreMiniWs2AutoCheckCom;

PT_SET.PreMiniWs1AutoCheckCom = cmbPreMiniWs1AutoCheckCom.Text.Trim();
PT_SET.PreMiniWs2AutoCheckCom = cmbPreMiniWs2AutoCheckCom.Text.Trim();
~~~

- [ ] **Step 3: Add save validation before assigning PT_SET or saving the INI**

~~~csharp
private bool ValidatePreMiniAutoCheckComs()
{
    string ws1Com = cmbPreMiniWs1AutoCheckCom.Text.Trim();
    string ws2Com = cmbPreMiniWs2AutoCheckCom.Text.Trim();
    if (chkPreMiniWs1AutoCheck.Checked && string.IsNullOrEmpty(ws1Com))
    {
        MessageBox.Show("右工站已启用自动点检，请设置综合点检COM。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }
    if (chkPreMiniWs2AutoCheck.Checked && string.IsNullOrEmpty(ws2Com))
    {
        MessageBox.Show("左工站已启用自动点检，请设置综合点检COM。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }
    if (chkPreMiniWs1AutoCheck.Checked && chkPreMiniWs2AutoCheck.Checked &&
        string.Equals(ws1Com, ws2Com, StringComparison.OrdinalIgnoreCase))
    {
        MessageBox.Show("左右工站不能使用同一个综合点检COM。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }
    return true;
}
~~~

Call this from `btn_ptset_save_Click` before each existing `GetData()` call (currently at `FrSys.cs:1047` and `FrSys.cs:1110`); return early when it fails so `PT_SET.SavePtCfg` cannot run.

- [ ] **Step 4: Merge the manual actions into one integrated read per station**

Replace separate distance and Lux/CCT readers with:

~~~csharp
private void ManualReadPreMiniAutoCheck(string stationName, string com)
{
    if (string.IsNullOrWhiteSpace(com))
    {
        MessageBox.Show(string.Format("{0}综合点检COM未设置", stationName), "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
    try
    {
        Cursor.Current = Cursors.WaitCursor;
        PreMiniAutoChecker.AutoCheckReading value = PreMiniAutoChecker.ManualReadAll(com);
        MessageBox.Show(string.Format("{0}综合点检读取完成\r\nCOM:{1}\r\nDistance:{2:F3}\r\nLux:{3:F3}\r\nCCT:{4:F3}",
            stationName, com, value.Distance, value.Lux, value.Cct), "pre-mini手动点检",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}综合点检读取失败,COM:{1},Err:{2}", stationName, com, ex.Message));
        MessageBox.Show(string.Format("{0}综合点检读取失败\r\nCOM:{1}\r\n{2}", stationName, com, ex.Message),
            "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        Cursor.Current = Cursors.Default;
    }
}
~~~

Each new click handler supplies the relevant integrated COM. The ManualReadAll API is added in Task 3.

- [ ] **Step 5: Verify the UI conversion and commit**

Run:

~~~powershell
rg -n 'PreMiniWs[12](DistanceCom|LuxCctCom[123])|btnPreMiniWs[12]Read(Distance|LuxCct)' UI/FormView/FrSys.cs UI/FormView/FrSys.Designer.cs
rg -n 'PreMiniWs[12]AutoCheckCom|ReadAutoCheck|ValidatePreMiniAutoCheckComs' UI/FormView/FrSys.cs UI/FormView/FrSys.Designer.cs
git add UI/FormView/FrSys.cs UI/FormView/FrSys.Designer.cs
git commit -m "feat: simplify pre-mini inspection COM settings"
~~~

Expected: the first search has no matches; the second reports both controls, validation, and read handlers; only the two form files are committed.

### Task 3: Reuse one serial session for distance and three optical reads

**Files:**

- Modify: UI/Class/PreMiniAutoChecker.cs lines 14-336

- [ ] **Step 1: Keep the established protocol and define a combined manual result**

Add inside PreMiniAutoChecker:

~~~csharp
public sealed class AutoCheckReading
{
    public double Distance { get; set; }
    public double Lux { get; set; }
    public double Cct { get; set; }
}
~~~

Do not change the packet header, baud rate, command IDs 0x8001, 0x7002, 0x8003, or JSON response parsing in SensorReader.

- [ ] **Step 2: Implement one-session manual reading**

~~~csharp
public static AutoCheckReading ManualReadAll(string com)
{
    if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");
    using (SensorReader reader = new SensorReader(com))
    {
        try
        {
            reader.SetLaserStatus(true);
            Thread.Sleep(1000);
            double distance = reader.ReadDistance(30000);
            Tuple<double, double> light = reader.ReadLuxCct(20000);
            return new AutoCheckReading { Distance = distance, Lux = light.Item1, Cct = light.Item2 };
        }
        finally
        {
            reader.SetLaserStatus(false);
        }
    }
}
~~~

- [ ] **Step 3: Open one COM in RunPointCheck**

Use the station mapping below and return EM_RES.PARA_ERR when the selected COM is empty:

~~~csharp
string com = station.id == 0 ? PT_SET.PreMiniWs1AutoCheckCom : PT_SET.PreMiniWs2AutoCheckCom;
if (string.IsNullOrWhiteSpace(com))
{
    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}综合点检失败:未设置COM", station.disc, pos.Name));
    return EM_RES.PARA_ERR;
}
using (SensorReader reader = new SensorReader(com))
{
    // Call the distance stage and then the optical stage.
}
~~~

Change CheckDistance and CheckLuxCct to receive the existing reader and com. Neither method may create its own serial port.

- [ ] **Step 4: Match ordinary MINI distance retry behavior**

One distance attempt performs two reads after laser enable and uses the second reading. Make no more than three attempts:

~~~csharp
for (int attempt = 1; attempt <= 3; attempt++)
{
    reader.ReadDistance(30000);
    Thread.Sleep(1000);
    double value = reader.ReadDistance(60000);
    double checkValue = value + pos.DistanceComp;
    if (pos.DistanceThreshold <= 0 || Math.Abs(checkValue - pos.ActualDistanceParam) <= pos.DistanceThreshold)
    {
        return EM_RES.OK;
    }
}
return EM_RES.ERR;
~~~

Use finally to always disable the laser. Timeouts, parse failures, and missing COM are failures, never skipped successes.

- [ ] **Step 5: Collect exactly three Lux/CCT readings**

Keep station-specific Z1/Z2/Z3 selection, but replace the three-COM nested loop with:

~~~csharp
for (int moveIndex = 0; moveIndex < zList.Length; moveIndex++)
{
    EM_RES moveRes = MT.Move(ref VAR.gsys_set.bquit, ref station.ax_z_fr, zList[moveIndex]);
    if (moveRes != EM_RES.OK) return moveRes;
    Thread.Sleep(2000);
    Tuple<double, double> value = reader.ReadLuxCct(20000);
    double lux = value.Item1;
    double cct = value.Item2;
    int pointIndex = moveIndex + 1;
    SaveToTxt(station.disc, pos.Name, "LuxCCT", pointIndex, com, zList[moveIndex].ToString("F3"), lux.ToString("F3"), cct.ToString("F3"));
    if (pos.LuxThreshold > 0 && Math.Abs(lux - pos.ActualLuxParam) > pos.LuxThreshold) return EM_RES.ERR;
    if (pos.CctThreshold > 0 && Math.Abs(cct - pos.ActualCctParam) > pos.CctThreshold) return EM_RES.ERR;
}
~~~

Use a finally block to move back to pos.Z1 after a successful or failed optical scan. No uniformity collections or calculations are added.

- [ ] **Step 6: Add stage-specific remeasure-or-stop handling**

In RunPointCheck, retry distance and optical stages independently:

~~~csharp
while (true)
{
    EM_RES distanceRes = CheckDistance(reader, station, pos, com);
    if (distanceRes == EM_RES.OK) break;
    if (!ShowRetryOrStop(station, pos, "距离")) return EM_RES.QUIT;
}
while (true)
{
    EM_RES lightRes = CheckLuxCct(reader, station, pos, com);
    if (lightRes == EM_RES.OK) break;
    if (!ShowRetryOrStop(station, pos, "光源色温")) return EM_RES.QUIT;
}
~~~

Implement `ShowRetryOrStop` with the existing customizable warning dialog so the visible actions are exactly `重新测量` and `停止`:

~~~csharp
private static bool ShowRetryOrStop(WSFEED station, WSFEED.PosDef pos, string stage)
{
    MT.ST_WARN warn = new MT.ST_WARN();
    warning dialog = new warning();
    warn.ok_txt = "重新测量";
    warn.abort_txt = "停止";
    warn.title = "pre-mini自动点检";
    warn.msg = string.Format("{0} {1}{2}点检失败", station.disc, pos.Name, stage);
    warn.lb_msg = warn.msg + "\\r\\n重新测量：重新执行本阶段。\\r\\n停止：停止测试。";
    return MT.Display_frwarn(dialog, warn, ERR_ALM.EmErrItem.CaptureAbnomal) == DialogResult.Yes;
}
~~~

It must not offer a skip path.

- [ ] **Step 7: Verify the runner conversion and commit**

Run:

~~~powershell
rg -n 'PreMiniWs[12](DistanceCom|LuxCctCom[123])|new string\[\].*LuxCctCom|YesNoCancel|跳过该点' UI/Class/PreMiniAutoChecker.cs
rg -n 'PreMiniWs[12]AutoCheckCom|ManualReadAll|ShowRetryOrStop|pointIndex = moveIndex \+ 1' UI/Class/PreMiniAutoChecker.cs
git add UI/Class/PreMiniAutoChecker.cs
git commit -m "feat: run pre-mini inspection through one COM"
~~~

Expected: the first search has no matches; the second confirms the integrated COM, combined manual read, retry/stop helper, and three-point indexing.

### Task 4: Stop rather than skip and verify on the bench

**Files:**

- Modify: UI/Class/WSFEED.cs lines 2682-2714
- Verify: Pre-Mini.sln

- [ ] **Step 1: Remove the scheduler retry/skip dialog**

Replace RunPreMiniAutoCheckPoint with:

~~~csharp
private EM_RES RunPreMiniAutoCheckPoint(PosDef pos)
{
    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}pre-mini自动点检单点执行,ID:{2}", disc, pos.Name, pos.ID));
    EM_RES res = PreMiniAutoChecker.RunPointCheck(this, pos);
    if (res != EM_RES.OK)
    {
        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}pre-mini自动点检停止,ID:{2},Res:{3}", disc, pos.Name, pos.ID, res));
    }
    return res;
}
~~~

The checker owns its two retry dialogs. WSFEED must not convert a failed point inspection to EM_RES.OK.

- [ ] **Step 2: Rebuild before connecting hardware**

Run:

~~~powershell
msbuild Pre-Mini.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /m
~~~

Expected: exit code 0 and successful MotionCtrl and UI builds.

- [ ] **Step 3: Run the hardware acceptance matrix**

| Case | Setup | Expected result |
|---|---|---|
| Valid data | Unique COMs; all values in tolerance | One distance log plus Z1/Z2/Z3 Lux/CCT logs; position completes |
| Empty COM | Enable a station but leave COM blank | Settings save warns and does not persist |
| Duplicate COM | Enable both stations with the same COM | Settings save warns and does not persist |
| Transient distance NG | First attempts out of tolerance, later valid | Automatic retry; no dialog |
| Permanent distance NG | Three failed attempts | Only remeasure or stop; stop prevents the next position |
| Z2 light NG | Lux or CCT fails at Z2 | Axis returns to Z1; remeasure starts again at Z1 or stop ends flow |
| Read timeout | Disconnect sensor during a read | No success record; only remeasure or stop |

- [ ] **Step 4: Verify the saved product configuration**

After a valid settings save, run:

~~~powershell
rg -n 'PreMiniWs[12](AutoCheckCom|DistanceCom|LuxCctCom)' UI/bin/product/*/ptcfg.ini
~~~

Expected: the two new keys are present. Historic old keys may remain in the file because this change intentionally does not delete user configuration; the source-level searches in Tasks 1–3 prove those keys are ignored.

- [ ] **Step 5: Commit only the scheduler source change**

~~~powershell
git add UI/Class/WSFEED.cs
git commit -m "fix: stop pre-mini inspection instead of skipping failures"
~~~

Expected: do not stage generated binaries, .vs, logs, product test data, or unrelated pre-existing modifications.
