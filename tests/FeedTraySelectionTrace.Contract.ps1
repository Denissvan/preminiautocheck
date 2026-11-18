$sourcePath = Join-Path $PSScriptRoot '..\UI\Class\UpDownLoad.cs'
$source = Get-Content -Raw -Encoding UTF8 $sourcePath
$marker = ([char]0x4F9B).ToString() + ([char]0x6599).ToString() + ([char]0x76D8).ToString() + ([char]0x9009).ToString() + ([char]0x6599).ToString() + ([char]0x8FFD).ToString() + ([char]0x8E2A).ToString()
$logIndex = $source.IndexOf($marker, [System.StringComparison]::Ordinal)
$moveIndex = $source.IndexOf('res = xt.XtCapMovMod(', $logIndex, [System.StringComparison]::Ordinal)

if ($logIndex -lt 0) { throw 'Missing feed-tray selection trace log.' }
if ($moveIndex -lt 0) { throw 'Missing XtCapMovMod call after the selection trace log.' }

foreach ($field in @('P0=[Idx=', 'selectedPos', 'p0Pos', 'pickOrder')) {
    if ($source.IndexOf($field, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Selection trace is missing field marker: $field"
    }
}

if ($source.IndexOf('Find(s => s.idx == 0)', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'P0 must be located by idx == 0 rather than list order.'
}

Write-Host 'PASS: Feed tray selection trace contract'
