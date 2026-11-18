$repo = Split-Path -Parent $PSScriptRoot
$checker = Get-Content -Raw (Join-Path $repo 'UI\Class\PreMiniAutoChecker.cs')
$form = Get-Content -Raw (Join-Path $repo 'UI\FormView\FrSys.cs')
$designer = Get-Content -Raw (Join-Path $repo 'UI\FormView\FrSys.Designer.cs')

$required = @(
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public static double ManualReadDistance(string com)' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public static Tuple<double, double> ManualReadLuxCct(string com)' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'ManualDistance' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'ManualLuxCct' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public static double ManualReadLux(string com)' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public static double ManualReadCct(string com)' },
    @{ File = 'FrSys.cs'; Content = 'btnPreMiniWs1ReadLuxCct_Click' },
    @{ File = 'FrSys.cs'; Content = 'btnPreMiniWs2ReadLuxCct_Click' },
    @{ File = 'FrSys.cs'; Content = 'btnPreMiniWs1ReadCct_Click' },
    @{ File = 'FrSys.cs'; Content = 'btnPreMiniWs2ReadCct_Click' },
    @{ File = 'FrSys.Designer.cs'; Content = 'btnPreMiniWs1ReadLuxCct.Click += new System.EventHandler(this.btnPreMiniWs1ReadLuxCct_Click);' },
    @{ File = 'FrSys.Designer.cs'; Content = 'btnPreMiniWs2ReadLuxCct.Click += new System.EventHandler(this.btnPreMiniWs2ReadLuxCct_Click);' },
    @{ File = 'FrSys.Designer.cs'; Content = 'btnPreMiniWs1ReadLuxCct2.Click += new System.EventHandler(this.btnPreMiniWs1ReadCct_Click);' },
    @{ File = 'FrSys.Designer.cs'; Content = 'btnPreMiniWs2ReadLuxCct2.Click += new System.EventHandler(this.btnPreMiniWs2ReadCct_Click);' }
)

$contents = @{
    'PreMiniAutoChecker.cs' = $checker
    'FrSys.cs' = $form
    'FrSys.Designer.cs' = $designer
}

$missing = @($required | Where-Object { -not $contents[$_.File].Contains($_.Content) })
if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Host ("MISSING: {0} -> {1}" -f $_.File, $_.Content) }
    exit 1
}

Write-Host 'PASS: independent pre-mini manual distance, Lux, and CCT reads are wired to the UI.'
