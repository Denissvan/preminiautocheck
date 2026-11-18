using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("Pre-Mini")]
[assembly: AssemblyDescription("装备与自动化部电气技术课")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sunny")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//将 ComVisible 设置为 false 将使此程序集中的类型
//对 COM 组件不可见。  如果需要从 COM 访问此程序集中的类型，
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("77c148f5-418a-4fb4-af50-deb5934664e3")]

// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
//可以指定所有这些值，也可以使用“生成号”和“修订号”的默认值，
// 方法是按如下所示使用“*”: :
// [assembly: AssemblyVersion("1.0.*")]
// 1.28.5.8: 增加启动普通版/料仓互换版选择，OK/NG 逻辑料仓可映射至对方实体料仓。
// 1.28.5.9: 上相机条码二次回检与下相机对齐；首次异常单点重拍，读空报警可重拍或停止，非空错码标记为111。
// 1.28.5.10: 停止恢复后优先完成工站二次回检，未完成前禁止进入下一批取放料。
// 1.28.5.11: 放回料盘后清理该模组待回检状态；记录供料盘选料与 P0 状态以追踪首次拍照位置。
// 1.28.5.12: pre-mini 点检支持串口分包、三通道 Lux/CCT 采集；自动三 Z 点各采集三通道，不做九点均匀度判定。
// 1.28.5.13: pre-mini 点检对齐 MINI，距离与光源色温独立使能；色温仅在色温测点执行三点 Z 测量。
// 1.28.5.14: MES/SECS 统计默认包含复测 OK、NG 数据。
[assembly: AssemblyVersion("1.28.5.14")]
[assembly: AssemblyFileVersion("1.28.5.14")]
