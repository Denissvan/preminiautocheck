# Pre-Mini 环境点检双模式定时 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 Pre-Mini 自动点检提供与普通 MINI 一致的固定时间和按小时周期两种触发模式。

**Architecture:** `EnvironmentInspectionSchedule` 是无 UI、无设备依赖的时间窗口判定器。`FrMain` 每秒调用该判定器并只设置现有左右工站待点检标志；`FrSys` 动态追加模式和周期控件，`PT_SET` 负责保存配置。既有 `WSFEED` 和 `PreMiniAutoChecker` 不改动。

**Tech Stack:** C#、.NET Framework WinForms、现有 INI 配置类、MSBuild Debug|x64。

---

## 文件结构

- 新增 `UI/Class/EnvironmentInspectionSchedule.cs`：纯时间窗口调度算法和两种模式枚举。
- 修改 `UI/UI.csproj`：编译新调度类。
- 修改 `MotionCtrl/Class/MyType.cs`：增加并持久化 `EnvironmentCheckScheduleMode`、`CheckTimer`。
- 修改 `UI/FormView/FrMain.cs`：使用调度器替换 Pre-Mini 的字符串相等触发。
- 修改 `UI/FormView/FrSys.cs`：在现有 Pre-Mini 点检分组内动态创建模式/周期控件并读写配置。

不创建 MSTest 项目；用户已明确排除该项目。验证由纯算法源码检查、配置键检查以及全量编译完成。

### Task 1: 增加可复用的时间窗口调度器

**Files:**
- Create: `UI/Class/EnvironmentInspectionSchedule.cs`
- Modify: `UI/UI.csproj:334-340`
- Test: 不创建独立测试项目（用户要求）

- [ ] **Step 1: 写入调度器实现前的静态验收场景**

在计划说明中锁定下列输入输出，实施后以调试器或临时调用验证：

```text
FixedTime: 08:59:59 -> 09:00:01, times 09:00/20:00 => due=true, dueTime=09:00
HourlyInterval: 11:59:59 -> 12:00:01, interval 12 => due=true, dueTime=12:00
HourlyInterval: 23:59:59 -> 次日00:00:01, interval 12 => due=true, dueTime=次日00:00
FixedTime: fixedTime1/fixedTime2 无法解析 => FrMain 不调用 HasDueTime
current <= previous => due=false
```

- [ ] **Step 2: 新增纯调度器代码**

创建 `UI/Class/EnvironmentInspectionSchedule.cs`，内容如下：

```csharp
using System;

namespace UI.Class
{
    public enum EnvironmentInspectionScheduleMode
    {
        FixedTime = 0,
        HourlyInterval = 1
    }

    public static class EnvironmentInspectionSchedule
    {
        public static bool HasDueTime(DateTime previous, DateTime current,
            EnvironmentInspectionScheduleMode mode, TimeSpan fixedTime1,
            TimeSpan fixedTime2, int intervalHours, out DateTime dueTime)
        {
            dueTime = DateTime.MinValue;
            if (current <= previous) return false;

            bool found = false;
            for (DateTime day = previous.Date; day <= current.Date; day = day.AddDays(1))
            {
                if (mode == EnvironmentInspectionScheduleMode.HourlyInterval)
                {
                    int hours = Math.Max(1, Math.Min(24, intervalHours));
                    for (int hour = hours; hour <= 24; hour += hours)
                        SetDueTime(day.AddHours(hour), previous, current, ref found, ref dueTime);
                }
                else
                {
                    SetDueTime(day.Add(fixedTime1), previous, current, ref found, ref dueTime);
                    SetDueTime(day.Add(fixedTime2), previous, current, ref found, ref dueTime);
                }
            }
            return found;
        }

        private static void SetDueTime(DateTime candidate, DateTime previous,
            DateTime current, ref bool found, ref DateTime dueTime)
        {
            if (candidate > previous && candidate <= current)
            {
                if (!found || candidate > dueTime) dueTime = candidate;
                found = true;
            }
        }
    }
}
```

- [ ] **Step 3: 将文件加入 WinForms 项目**

在 `UI/UI.csproj` 的现有 `Class` 编译项附近加入：

```xml
<Compile Include="Class\EnvironmentInspectionSchedule.cs" />
```

- [ ] **Step 4: 核对算法覆盖场景**

运行：

```powershell
rg -n 'HourlyInterval|FixedTime|day.AddHours\(hour\)|candidate > previous' UI\Class\EnvironmentInspectionSchedule.cs
```

预期：四个关键符号均能找到，算法只在 `(previous, current]` 窗口内触发。

### Task 2: 保存和读取双模式配置

**Files:**
- Modify: `MotionCtrl/Class/MyType.cs:1240-1242,1435-1455,1592-1609`
- Test: 不创建独立测试项目（用户要求）

- [ ] **Step 1: 定义默认配置字段**

紧跟现有 `CheckTimeMorning1`、`CheckTimeEvening2` 字段增加：

```csharp
public static double CheckTimer = 12;
public static int EnvironmentCheckScheduleMode = 0;
```

- [ ] **Step 2: 在加载配置时读取字段**

紧跟 `CHECKTIMEEVENING2` 的读取语句增加：

```csharp
CheckTimer = inf.ReadDouble("AutoCheckParam", "CheckTimer", 12);
EnvironmentCheckScheduleMode = inf.ReadInteger("AutoCheckParam", "EnvironmentCheckScheduleMode", 0);
```

- [ ] **Step 3: 在保存配置时写入字段**

紧跟 `CHECKTIMEEVENING2` 的写入语句增加：

```csharp
inf.WriteDouble("AutoCheckParam", "CheckTimer", CheckTimer, ref ischange, true, filename);
inf.WriteInteger("AutoCheckParam", "EnvironmentCheckScheduleMode", EnvironmentCheckScheduleMode, ref ischange, true, filename);
```

- [ ] **Step 4: 检查配置对称性**

运行：

```powershell
rg -n 'CheckTimer|EnvironmentCheckScheduleMode' MotionCtrl\Class\MyType.cs
```

预期：各字段都有声明、`Read*` 和 `Write*` 三处引用。

### Task 3: 将 Pre-Mini 定时触发改为窗口判定

**Files:**
- Modify: `UI/FormView/FrMain.cs:50,69-70,849-864`
- Test: 不创建独立测试项目（用户要求）

- [ ] **Step 1: 用上次检查时间替换字符串防重字段**

将：

```csharp
private string preMiniAutoCheckLastTriggerTime = "";
```

替换为：

```csharp
private DateTime preMiniAutoCheckLastScheduleCheck;
```

在构造函数中、注册 `timer_sysTimeCnt.Tick` 后初始化：

```csharp
preMiniAutoCheckLastScheduleCheck = DateTime.Now;
```

- [ ] **Step 2: 替换 Tick 方法中的固定时间字符串比较**

将 `timer_sysTimeCnt_Tick` 的主体替换为：

```csharp
if (!PT_SET.PreMiniAutoCheckWs1En && !PT_SET.PreMiniAutoCheckWs2En) return;

DateTime now = DateTime.Now;
DateTime previous = preMiniAutoCheckLastScheduleCheck;
preMiniAutoCheckLastScheduleCheck = now;
EnvironmentInspectionScheduleMode mode =
    PT_SET.EnvironmentCheckScheduleMode == (int)EnvironmentInspectionScheduleMode.HourlyInterval
        ? EnvironmentInspectionScheduleMode.HourlyInterval
        : EnvironmentInspectionScheduleMode.FixedTime;

TimeSpan fixedTime1 = TimeSpan.Zero;
TimeSpan fixedTime2 = TimeSpan.Zero;
if (mode == EnvironmentInspectionScheduleMode.FixedTime &&
    (!TimeSpan.TryParse(PT_SET.CheckTimeMorning1, out fixedTime1) ||
     !TimeSpan.TryParse(PT_SET.CheckTimeEvening2, out fixedTime2))) return;

DateTime dueTime;
if (!EnvironmentInspectionSchedule.HasDueTime(previous, now, mode, fixedTime1,
    fixedTime2, (int)Math.Round(PT_SET.CheckTimer), out dueTime)) return;

VAR.PreMiniAutoCheckTimeReached = true;
if (PT_SET.PreMiniAutoCheckWs1En) VAR.PreMiniAutoCheckWs1Pending = true;
if (PT_SET.PreMiniAutoCheckWs2En) VAR.PreMiniAutoCheckWs2Pending = true;
string modeText = mode == EnvironmentInspectionScheduleMode.HourlyInterval ? "按小时周期" : "固定时间";
VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
    string.Format("pre-mini自动点检时间到,模式:{0},计划时间:{1:yyyy-MM-dd HH:mm:ss}", modeText, dueTime));
```

- [ ] **Step 3: 验证触发出口未改变**

运行：

```powershell
rg -n 'PreMiniAutoCheckWs1Pending = true|PreMiniAutoCheckWs2Pending = true|RunPreMiniAutoCheckIfNeeded' UI\FormView\FrMain.cs UI\Class\WSFEED.cs
```

预期：`FrMain` 仍只置现有待执行标志，`WSFEED` 仍为唯一执行入口。

### Task 4: 在 Pre-Mini 设置页加入模式和周期控件

**Files:**
- Modify: `UI/FormView/FrSys.cs:28-34,584-600,800-814`
- Test: 不创建独立测试项目（用户要求）

- [ ] **Step 1: 声明并创建控件**

在 `FrSys` 类字段区声明：

```csharp
private RadioButton rbtnPreMiniFixedTime;
private RadioButton rbtnPreMiniHourly;
private NumericUpDown nudPreMiniAutoCheckTimer;
private Label labelPreMiniAutoCheckTimer;
```

在构造函数 `InitializeComponent()` 后调用 `InitPreMiniAutoCheckScheduleControls()`。该方法创建并加入 `groupBoxPreMiniAutoCheck`：固定时间单选项位置 `(24, 286)`，按小时周期单选项位置 `(160, 286)`，周期数值框位置 `(350, 282)`，标签位置 `(455, 286)`；周期数值框限定 `Minimum=1`、`Maximum=24`、`DecimalPlaces=0`，标签文本为 `小时`。

- [ ] **Step 2: 绑定模式变化行为**

新增以下方法，使模式切换立即更新界面：

```csharp
private void UpdatePreMiniAutoCheckScheduleControls()
{
    bool fixedTime = rbtnPreMiniFixedTime.Checked;
    dtpPreMiniAutoCheckTime1.Enabled = fixedTime;
    dtpPreMiniAutoCheckTime2.Enabled = fixedTime;
    labelPreMiniAutoCheckTime1.Enabled = fixedTime;
    labelPreMiniAutoCheckTime2.Enabled = fixedTime;
    nudPreMiniAutoCheckTimer.Enabled = !fixedTime;
    labelPreMiniAutoCheckTimer.Enabled = !fixedTime;
    PT_SET.EnvironmentCheckScheduleMode = rbtnPreMiniHourly.Checked ? 1 : 0;
}
```

两项 `CheckedChanged` 都绑定到调用此方法的事件处理器。

- [ ] **Step 3: 在 ShowData 和 GetData 中同步配置**

在 `ShowData()` 设置两个时间控件后追加：

```csharp
rbtnPreMiniHourly.Checked = PT_SET.EnvironmentCheckScheduleMode == 1;
rbtnPreMiniFixedTime.Checked = !rbtnPreMiniHourly.Checked;
nudPreMiniAutoCheckTimer.Value = Math.Max(1, Math.Min(24, (decimal)PT_SET.CheckTimer));
UpdatePreMiniAutoCheckScheduleControls();
```

在 `GetData()` 保存两个时间字符串后追加：

```csharp
PT_SET.CheckTimer = (double)nudPreMiniAutoCheckTimer.Value;
PT_SET.EnvironmentCheckScheduleMode = rbtnPreMiniHourly.Checked ? 1 : 0;
```

- [ ] **Step 4: 验证界面模式互斥和范围**

运行：

```powershell
rg -n 'rbtnPreMini(FixedTime|Hourly)|nudPreMiniAutoCheckTimer\.(Minimum|Maximum)|EnvironmentCheckScheduleMode' UI\FormView\FrSys.cs
```

预期：固定时间和周期单选项都存在，周期限制为 1 到 24，保存时写入模式和周期。

### Task 5: 全量编译与交付前检查

**Files:**
- Verify: `Pre-Mini.sln`

- [ ] **Step 1: 扫描旧的精确时间比较实现是否已移除**

运行：

```powershell
rg -n 'nowTime != PT_SET\.CheckTimeMorning1|preMiniAutoCheckLastTriggerTime' UI\FormView\FrMain.cs
```

预期：无匹配。

- [ ] **Step 2: 运行全量 Rebuild**

运行：

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' Pre-Mini.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /m /nologo
```

预期：退出码 0，`0 个错误`。现有项目警告可保留，但不新增编译错误。

- [ ] **Step 3: 现场验证清单**

在不改变其他工站配置的前提下验证：固定模式在两个设定时间各触发一次；小时周期设为 12 时，在 12:00 和 24:00 置该工站待点检标志；左右工站只在各自启用时进入待点检；到达起始工位后沿用既有单 COM 点检流程。
