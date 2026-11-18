# Pre-Mini 料仓互换版 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 Pre-Mini 增加与 MINI 一致的开机“普通版本 / 料仓互换版”选择；互换模式中，逻辑 OK/NG 料仓改用对方实体料仓的轴、I/O 和气缸，而既有普通模式行为不变。

**Architecture:** 在 `Program.Main` 创建任何 `MT` 或 `COM` 静态对象前选择只在本次进程有效的 `RuntimeMachineMode`。`COM` 用两个集中工厂方法创建逻辑 OK/NG `TrayBox`，互换模式仅交换其硬件依赖；上下料流程继续只使用 `COM.traybox_ok` / `COM.traybox_ng`。现有页面不动态新增控件，Designer 维持普通版默认布局，窗体加载时仅重排现有 OK/NG 控件并更新标签。

**Tech Stack:** C#、.NET Framework WinForms、现有 MotionCtrl/TrayBox 硬件抽象、Visual Studio/MSBuild。

---

## 机型映射合同

| 逻辑动作 | 普通版本实体资源 | 料仓互换版实体资源 |
|---|---|---|
| 放 OK | `AXIS_UDL_OK_X/Z`、`*_OK_*` I/O、`CLD_UDL_OKTRAY_HD` | `AXIS_UDL_NG_X/Z`、`*_NG_*` I/O、`CLD_UDL_NGTRAY_HD` |
| 放 NG | `AXIS_UDL_NG_X/Z`、`*_NG_*` I/O、`CLD_UDL_NGTRAY_HD` | `AXIS_UDL_OK_X/Z`、`*_OK_*` I/O、`CLD_UDL_OKTRAY_HD` |
| 供料 | 不变 | 不变 |

`TrayBox_OK` 与 `TrayBox_NG` 的配置文件身份保持为逻辑身份，避免因选择机型而把 OK/NG 的料盘参数写进对方配置文件；只交换其构造时注入的物理硬件资源。

## 将改动的文件

- 修改：`UI/Class/Program.cs` — 开机选择与运行时机型状态。
- 修改：`UI/Class/MT.cs` — 让 OK/NG 实体轴的显示名称在互换模式下反映实际槽位。
- 修改：`UI/Class/myfunc.cs` — 集中创建 OK/NG 逻辑料仓、加载时记录映射。
- 修改：`UI/FormView/FrRun.cs` — 运行页中的 OK/NG 料仓卡片、料盘卡片和换料按钮按实体槽位显示。
- 修改：`UI/FormView/FrProduct.cs` — 参数页选项卡、当前 `TrayBox` 绑定与学习/保存目标按逻辑料仓正确对应。
- 修改：`UI/FormView/FrRst.cs` — 复位页 OK/NG 手动轴按钮、状态灯和位置文本绑定当前实体槽位。
- 修改：`UI/Properties/AssemblyInfo.cs` — 增加本次机型互换功能的版本备注。
- 不修改：`UI/FormView/*.Designer.cs` — 仅使用现有控件；默认普通版位置由 Designer 保持，互换版在窗体代码中移动已有控件。

## Task 1: 启动模式在硬件静态初始化前确定

**Files:**
- Modify: `UI/Class/Program.cs:8-35`
- Test: 手工启动验证（本工程按既有决定不新增 MSTest 项目）

- [ ] **Step 1: 在 `namespace UI` 中定义机型状态类型**

```csharp
public enum MachineStartupMode
{
    Normal = 0,
    TrayBoxSwapped = 1
}

public static class RuntimeMachineMode
{
    public static MachineStartupMode Current { get; private set; } = MachineStartupMode.Normal;
    public static bool IsTrayBoxSwapped => Current == MachineStartupMode.TrayBoxSwapped;

    public static void Set(MachineStartupMode mode)
    {
        Current = mode;
    }
}
```

- [ ] **Step 2: 在 `Program` 增加选择函数；取消必须直接退出而不启动 Secs 或主窗体**

```csharp
private static bool SelectMachineMode()
{
    DialogResult result = MessageBox.Show(
        "请选择机台版本:\r\n是：普通版本\r\n否：料仓互换版\r\n取消：退出程序",
        "机台版本选择",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);

    if (result == DialogResult.Cancel) return false;

    RuntimeMachineMode.Set(result == DialogResult.No
        ? MachineStartupMode.TrayBoxSwapped
        : MachineStartupMode.Normal);
    return true;
}
```

- [ ] **Step 3: 将 `Main` 的第一项业务操作改为调用选择函数**

```csharp
static void Main()
{
    if (!SelectMachineMode()) return;

    Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\Release\SecsApp.exe");
    // 保留原有单实例检查、ImageSaveQueue.gInit 和 Application.Run 流程。
}
```

- [ ] **Step 4: 手工验证三个启动分支**

运行 `UI\bin\x64\Pre-Mini.exe`：选择“是”后继续启动；选择“否”后继续启动；选择“取消”后确认不出现 `SecsApp.exe` 且不创建主窗体。

## Task 2: 集中创建逻辑料仓并交换实体硬件资源

**Files:**
- Modify: `UI/Class/MT.cs:57-81`
- Modify: `UI/Class/myfunc.cs:24-43, 103-118`
- Test: 启动日志与调试器对象检查

- [ ] **Step 1: 更新 `MT.cs` 的 OK/NG 轴显示名，通道号不变**

```csharp
public static AXIS AXIS_UDL_OK_X = new AXIS(1, CARD_ECI2400,
    RuntimeMachineMode.IsTrayBoxSwapped ? "NG料X轴" : "OK料X轴",
    RuntimeMachineMode.IsTrayBoxSwapped ? "NG_X" : "OK_X",
    AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
public static AXIS AXIS_UDL_NG_X = new AXIS(2, CARD_ECI2400,
    RuntimeMachineMode.IsTrayBoxSwapped ? "OK料X轴" : "NG料X轴",
    RuntimeMachineMode.IsTrayBoxSwapped ? "OK_X" : "NG_X",
    AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
```

对 `AXIS_UDL_OK_Z`、`AXIS_UDL_NG_Z` 应使用相同规则：保持 6/7 号物理通道不变，只令显示名反映其在当前机型下的实体槽位。

- [ ] **Step 2: 用工厂方法替换 `COM.traybox_ok` 与 `COM.traybox_ng` 的直接构造**

```csharp
private static TrayBox CreateTrayBoxOk()
{
    if (RuntimeMachineMode.IsTrayBoxSwapped)
        return new TrayBox("TrayBox_OK", "OK料仓", TrayBox.EM_DIR.IN_OUT, 10,
            MT.AXIS_UDL_NG_X, MT.AXIS_UDL_NG_Z,
            MT.GPIO_IN_DL_INP_NG_TRAYBOX, MT.GPIO_IN_DL_RDY_NG_TRAY,
            MT.GPIO_OUT_DL_NG_TRAY, MT.GPIO_IN_UDL_NG_TRAY_ON,
            MT.CLD_UDL_NGTRAY_HD);

    return new TrayBox("TrayBox_OK", "OK料仓", TrayBox.EM_DIR.IN_OUT, 10,
        MT.AXIS_UDL_OK_X, MT.AXIS_UDL_OK_Z,
        MT.GPIO_IN_DL_INP_OK_TRAYBOX, MT.GPIO_IN_DL_RDY_OK_TRAY,
        MT.GPIO_OUT_DL_OK_TRAY, MT.GPIO_IN_UDL_OK_TRAY_ON,
        MT.CLD_UDL_OKTRAY_HD);
}
```

以同样的完整参数镜像实现 `CreateTrayBoxNg()`：互换模式返回 OK 的实体轴/I/O/气缸，普通模式保持现有 NG 参数。随后改为：

```csharp
public static TrayBox traybox_ok = CreateTrayBoxOk();
public static TrayBox traybox_ng = CreateTrayBoxNg();
public static List<TrayBox> List_traybox = new List<TrayBox>
    { traybox_fd, traybox_ok, traybox_ng };
```

- [ ] **Step 3: 给 `COM` 增加模式与映射日志**

```csharp
public static string GetStartupModeDescription()
{
    return RuntimeMachineMode.IsTrayBoxSwapped ? "料仓互换版" : "普通版本";
}

public static string DescribeTrayBoxMapping(TrayBox traybox)
{
    return string.Format("{0}映射: 物理轴={1}/{2}, 配置={3}",
        traybox.disc, traybox.ax_x.disc, traybox.ax_z.disc,
        Path.GetFileName(traybox.strCfgPath));
}
```

在 `TrayBoxInit` 的循环前记录机型，在每个 `traybox.LoadCfg(productname)` 成功后记录 `DescribeTrayBoxMapping(traybox)`。

- [ ] **Step 4: 验证普通与互换模式的对象映射**

在普通模式确认 `COM.traybox_ok.ax_x == MT.AXIS_UDL_OK_X`、`COM.traybox_ng.ax_x == MT.AXIS_UDL_NG_X`；在互换模式确认二者相反。两种模式的日志均须显示 `TrayBox_OK` 使用其逻辑配置文件、`TrayBox_NG` 使用其逻辑配置文件。

## Task 3: 运行页按实体槽位显示逻辑 OK/NG 料仓

**Files:**
- Modify: `UI/FormView/FrRun.cs:25-180, 795-825`
- Test: WinForms 手工界面检查

- [ ] **Step 1: 增加 `ApplyTrayBoxUiMapping()`，只移动现有控件**

该方法在普通模式保留 Designer 中既有位置和文字；互换模式用 `tableLayoutPanel4.SetCellPosition` 交换 `traybox_ok`/`tray_ok` 与 `traybox_ng`/`tray_ng` 的行，并把两个“更换料盒”按钮放到相同实体槽位。不得 `new Button()`、不得向控件集合追加控件。

```csharp
private void ApplyTrayBoxUiMapping()
{
    if (!RuntimeMachineMode.IsTrayBoxSwapped) return;

    tableLayoutPanel4.SetCellPosition(traybox_ok, new TableLayoutPanelCellPosition(0, 2));
    tableLayoutPanel4.SetCellPosition(traybox_ng, new TableLayoutPanelCellPosition(0, 1));
    tableLayoutPanel4.SetCellPosition(tray_ok, new TableLayoutPanelCellPosition(1, 2));
    tableLayoutPanel4.SetCellPosition(tray_ng, new TableLayoutPanelCellPosition(1, 1));
}
```

- [ ] **Step 2: 在 `FrRun_Load` 的料仓对象绑定完成后调用映射方法**

确保 `traybox_ok.box` 始终为 `COM.traybox_ok`、`traybox_ng.box` 始终为 `COM.traybox_ng`；界面位置可以变，逻辑对象引用不得互换。

- [ ] **Step 3: 让换料确认文字说明逻辑料仓与实体槽位**

在 `btn_new_traybox_ok_Click`、`btn_new_traybox_ng_Click` 的提示文字中追加 `COM.DescribeTrayBoxMapping(...)`，使操作员在换盒前看到该动作会操作的实体轴。

- [ ] **Step 4: 手工验证运行页**

普通模式检查布局与改动前截图一致；互换模式检查 OK 卡片与 NG 卡片整行交换、点击 OK 换料只调用 `COM.traybox_ok.NewBox()`，点击 NG 换料只调用 `COM.traybox_ng.NewBox()`。

## Task 4: 参数页和复位页与映射保持一致

**Files:**
- Modify: `UI/FormView/FrProduct.cs:100-128, 446-468, 559-645`
- Modify: `UI/FormView/FrRst.cs:175-199`
- Test: WinForms 手工界面检查与手动轴点动验证

- [ ] **Step 1: 在 `FrProduct` 增加参数页绑定对象**

```csharp
private TrayBox GetTrayBoxForTab(string tabName)
{
    switch (tabName)
    {
        case "ctp_tray_ok": return COM.traybox_ok;
        case "ctp_tray_ng": return COM.traybox_ng;
        default: return COM.traybox_fd;
    }
}
```

将 `ctb_tray_cfg_SelectedIndexChanged`、`TrayCfgGotoPos`、当前位置读取和保存处理中的重复 `switch` 改为先取得此逻辑 `TrayBox`，再使用其 `ax_x`/`ax_z`。这样互换模式中点“OK料盘”仍保存 OK 配置，但运动到 NG 实体轴。

- [ ] **Step 2: 在参数页更新选项卡可见文字，不改变选项卡 Name**

普通模式：`ctp_tray_ok.Text = "  OK料盘  "`、`ctp_tray_ng.Text = "  NG料盘  "`；互换模式：`ctp_tray_ok.Text = "  OK料盘（NG位）  "`、`ctp_tray_ng.Text = "  NG料盘（OK位）  "`。保留 `ctp_tray_ok`、`ctp_tray_ng` 的 `Name`，确保第 1 步的绑定不受显示文案影响。

- [ ] **Step 3: 在 `FrRst` 使用 `TrayBox` 轴而非固定 `MT.AXIS_UDL_OK/NG_*` 更新状态**

```csharp
SetBtnStatusColor(btn_traybox_ok_z, COM.traybox_ok.ax_z.home_status, bflag);
SetBtnStatusColor(btn_traybox_ok_x, COM.traybox_ok.ax_x.home_status, bflag);
SetBtnStatusColor(btn_traybox_ng_z, COM.traybox_ng.ax_z.home_status, bflag);
SetBtnStatusColor(btn_traybox_ng_x, COM.traybox_ng.ax_x.home_status, bflag);
lb_box_ok.Text = COM.traybox_ok.StrOfPos;
lb_box_ng.Text = COM.traybox_ng.StrOfPos;
```

互换模式下通过既有 `Location` 属性交换 OK/NG 标签和点动按钮的行位置；普通模式不改位置。按钮事件也必须从 `COM.traybox_ok.ax_x/ax_z`、`COM.traybox_ng.ax_x/ax_z` 取得运动轴，不再直接引用固定 OK/NG `MT` 轴。

- [ ] **Step 4: 手工验证参数与复位页**

互换模式下，在“OK料盘”页保存一个不会与 NG 相同的测试参数，重启并再次选择互换版后确认仍从 `TrayBox_OK` 配置读取；在复位页点动 OK/NG X/Z，确认状态灯、坐标文本和实际实体槽位与运行页一致。

## Task 5: 上下料功能回归与构建交付

**Files:**
- Modify: `UI/Properties/AssemblyInfo.cs:版本记录注释区`
- Verify: `Pre-Mini.sln`

- [ ] **Step 1: 检查上下料流程只使用逻辑 TrayBox**

在 `UI/Class/UpDownLoad.cs`、`UI/Class/WSFEED.cs`、`UI/Class/myfunc.cs` 搜索 `traybox_ok`、`traybox_ng`、`AXIS_UDL_OK`、`AXIS_UDL_NG`。对于业务取放料，保留 `COM.traybox_ok` / `COM.traybox_ng`；若发现业务路径直接引用 `MT.AXIS_UDL_OK_*` 或 `MT.AXIS_UDL_NG_*`，改为对应 `TrayBox.ax_x` / `TrayBox.ax_z`，避免绕过映射。

- [ ] **Step 2: 添加版本记录**

在 `UI/Properties/AssemblyInfo.cs` 的历史注释区增加：

```csharp
// 增加启动普通版/料仓互换版选择，OK/NG 逻辑料仓可映射至对方实体料仓。
```

- [ ] **Step 3: 完整构建**

在仓库根目录运行：

```powershell
msbuild .\Pre-Mini.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64
```

期望：构建成功，新增错误为 0。现有历史警告仅在数量和类别未增加时可接受。

- [ ] **Step 4: 现场联锁验收（先空载、后带空料盘）**

在两种模式各执行一次：复位 → 供料仓换盒 → OK 料仓换盒 → NG 料仓换盒 → OK 下料 → NG 下料 → 满料/空料报警 → 急停 → 复位恢复。每次动作核对运行日志中的模式和映射；严禁在未确认实际轴位前装入产品料盘。

- [ ] **Step 5: 独立提交前检查**

由于当前工作树已有用户修改，先运行 `git status --short`，只暂存本计划涉及的源文件和版本记录，不暂存 `.vs`、`bin`、`obj`、日志或既有点检修改。确认差异后再由用户决定是否提交。

## 覆盖关系自查

- 开机模式与取消退出：Task 1。
- OK/NG 物理轴、I/O、气缸互换：Task 2。
- 逻辑配置文件不互换：Task 2、Task 4。
- 运行/参数/复位 UI 一致性且不动态创建控件：Task 3、Task 4。
- 上下料去向、换料、满空料、急停恢复、普通版回归：Task 5。

