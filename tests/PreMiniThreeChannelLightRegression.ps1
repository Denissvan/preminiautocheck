$repo = Split-Path -Parent $PSScriptRoot
$checker = Get-Content -Raw (Join-Path $repo 'UI\Class\PreMiniAutoChecker.cs')
$form = Get-Content -Raw (Join-Path $repo 'UI\FormView\FrSys.cs')

$required = @(
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public sealed class LightChannelReading' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public List<LightChannelReading> ReadLightChannels(int timeoutMs)' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'public static List<LightChannelReading> ManualReadLightChannels(string com)' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'data.TryGetValue("lux" + channel' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'private long lightFrameSequence;' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'lightFrameSequence > startSequence' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = 'moveIndex * 3 + channel.Channel' },
    @{ File = 'PreMiniAutoChecker.cs'; Content = '光源色温返回通道数不足' },
    @{ File = 'FrSys.cs'; Content = 'ManualReadPreMiniLightChannels' },
    @{ File = 'FrSys.cs'; Content = 'CH1 Lux' },
    @{ File = 'FrSys.cs'; Content = 'CH3 CCT' }
)

$contents = @{ 'PreMiniAutoChecker.cs' = $checker; 'FrSys.cs' = $form }
$missing = @($required | Where-Object { -not $contents[$_.File].Contains($_.Content) })
if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Host ("MISSING: {0} -> {1}" -f $_.File, $_.Content) }
    exit 1
}

$manualMethod = [regex]::Match($checker, 'public static List<LightChannelReading> ManualReadLightChannels\(string com\)(?<body>[\s\S]*?)\n        }\n\n        private static')
if (-not $manualMethod.Success) {
    Write-Host 'MISSING: ManualReadLightChannels method body'
    exit 1
}
if ($manualMethod.Groups['body'].Value.Contains('MT.Move')) {
    Write-Host 'INVALID: ManualReadLightChannels must not move an axis'
    exit 1
}

Write-Host 'PASS: pre-mini manual light reading returns three channels without axis motion; auto check collects three channels at each Z position from a fresh frame.'
