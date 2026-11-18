# pre-mini 手动传感器读取 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为每个 pre-mini 工站提供独立的距离和照度/色温手动读取按钮，并输出可诊断的串口日志。

**Architecture:** `PreMiniAutoChecker` 新增两个小型公开读取 API，负责各自的单项协议和日志；`FrSys` 事件处理只选择工站 COM、调用 API 并显示结果。Designer 复用现有同一行控件，避免改变群组尺寸和控件层级。

**Tech Stack:** C# .NET Framework WinForms、`System.IO.Ports.SerialPort`、Newtonsoft.Json。

---

### Task 1: 手动读取 API 的回归检查

**Files:**
- Create: `tests/PreMiniManualSensorReadRegression.ps1`
- Test: `tests/PreMiniManualSensorReadRegression.ps1`

- [ ] **Step 1: Write the failing test**

```powershell
# Assert that ManualReadDistance, ManualReadLuxCct and their UI click bindings exist.
```

- [ ] **Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File tests\PreMiniManualSensorReadRegression.ps1`

Expected: FAIL because the independent APIs and event bindings do not yet exist.

### Task 2: 独立读取和串口追踪

**Files:**
- Modify: `UI/Class/PreMiniAutoChecker.cs:20-157`

- [ ] **Step 1: Implement the minimum APIs**

```csharp
public static double ManualReadDistance(string com);
public static Tuple<double, double> ManualReadLuxCct(string com);
```

Each API creates one `SensorReader`, logs the manual operation, and performs only its own sensor command.

- [ ] **Step 2: Add diagnostic log points**

Log command sends, raw receive frames, discarded/incomplete frames, parse errors, and timeout counters only for a manual trace session.

### Task 3: UI bindings and layout

**Files:**
- Modify: `UI/FormView/FrSys.cs:903-938`
- Modify: `UI/FormView/FrSys.Designer.cs:4570-4648`

- [ ] **Step 1: Separate the event handlers**

```csharp
private void btnPreMiniWs1ReadDistance_Click(object sender, EventArgs e);
private void btnPreMiniWs1ReadLuxCct_Click(object sender, EventArgs e);
```

Repeat for the left station, using its own `cmbPreMiniWs2DistanceCom` value.

- [ ] **Step 2: Show the Lux/CCT controls**

Set its label and `Visible` state in Designer without moving any unrelated control.

### Task 4: Verify

**Files:**
- Test: `tests/PreMiniManualSensorReadRegression.ps1`

- [ ] **Step 1: Run regression check**

Run: `powershell -ExecutionPolicy Bypass -File tests\PreMiniManualSensorReadRegression.ps1`

Expected: PASS.

- [ ] **Step 2: Build**

Run: `MSBuild UI\UI.csproj /t:Build /p:Configuration=Debug`

Expected: exit code 0.
