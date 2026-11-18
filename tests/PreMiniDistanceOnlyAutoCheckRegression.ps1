$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$autoChecker = Get-Content -Raw -LiteralPath (Join-Path $repo 'UI\Class\PreMiniAutoChecker.cs')
$settings = Get-Content -Raw -LiteralPath (Join-Path $repo 'MotionCtrl\Class\MyType.cs')
$systemForm = Get-Content -Raw -LiteralPath (Join-Path $repo 'UI\FormView\FrSys.cs')
$designer = Get-Content -Raw -LiteralPath (Join-Path $repo 'UI\FormView\FrSys.Designer.cs')

function Assert-Contains([string]$Source, [string]$Pattern, [string]$Message)
{
    if ($Source -notmatch $Pattern)
    {
        throw $Message
    }
}

foreach ($name in @('PreMiniWs1DistanceCheckEn', 'PreMiniWs1LuxCctCheckEn', 'PreMiniWs2DistanceCheckEn', 'PreMiniWs2LuxCctCheckEn'))
{
    Assert-Contains $settings ("public static bool {0} = false;" -f $name) "缺少默认关闭的 $name 配置。"
    Assert-Contains $settings ('ReadBool\("AutoCheckParam", "{0}", false\)' -f $name) "缺少 $name 的配置读取。"
    Assert-Contains $settings ('WriteBool\("AutoCheckParam", "{0}", {0}, ref ischange, true, filename\)' -f $name) "缺少 $name 的配置保存。"
}

Assert-Contains $autoChecker 'private static bool NeedsDistanceCheck\(WSFEED station\)' '缺少 MINI 同款的距离点检工站使能判断。'
Assert-Contains $autoChecker 'private static bool NeedsLuxCctCheck\(WSFEED station, WSFEED\.PosDef pos\)' '缺少 MINI 同款的光源色温点检工站与测点判断。'
Assert-Contains $autoChecker 'pos\.Name\.Contains\(\"色温\"\)' '光源色温点检必须只对名称包含“色温”的测点执行。'
Assert-Contains $autoChecker 'if \(runDistance\)\s*\{\s*while \(CheckDistance' '距离点检必须受独立使能控制。'
Assert-Contains $autoChecker 'if \(runLuxCct\)\s*\{\s*while \(CheckLuxCct' '照度/色温点检必须受独立使能控制。'
if ($autoChecker -match 'return pos != null && \(pos\.LuxThreshold > 0 \|\| pos\.CctThreshold > 0\);')
{
    throw '不能再使用光照/色温阈值作为项目使能开关。'
}

foreach ($control in @('chkPreMiniWs1DistanceCheck', 'chkPreMiniWs1LuxCctCheck', 'chkPreMiniWs2DistanceCheck', 'chkPreMiniWs2LuxCctCheck'))
{
    Assert-Contains $designer ("this\.{0} = new System\.Windows\.Forms\.CheckBox\(\)" -f $control) "Designer 未创建 $control。"
    Assert-Contains $designer ("groupBoxPreMiniAutoCheck\.Controls\.Add\(this\.{0}\)" -f $control) "Designer 未将 $control 加入点检设置区。"
    Assert-Contains $systemForm ("{0}\.Checked = PT_SET\." -f $control) "设置页未加载 $control。"
    Assert-Contains $systemForm ("PT_SET\..* = {0}\.Checked" -f $control) "设置页未保存 $control。"
}

Write-Host 'PASS: pre-mini uses MINI-style explicit distance and Lux/CCT enable switches.'
