param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot "..\UI\bin\x64\Pre-Mini.exe")
)

$assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))
$type = $assembly.GetType("UI.BarcodeBackcheckPolicy", $false)
if ($null -eq $type) {
    throw "BarcodeBackcheckPolicy type is required."
}

$classify = $type.GetMethod("Classify")
if ($classify.Invoke($null, @("ABC", "ABC")).ToString() -ne "Match") {
    throw "Matching barcode must be Match."
}
if ($classify.Invoke($null, @("ABC", "")).ToString() -ne "Empty") {
    throw "Empty actual barcode must be Empty."
}
if ($classify.Invoke($null, @("ABC", "XYZ")).ToString() -ne "Mismatch") {
    throw "Different non-empty barcode must be Mismatch."
}
if ($classify.Invoke($null, @("", "XYZ")).ToString() -ne "Mismatch") {
    throw "Missing recorded barcode with a scanned value must be Mismatch."
}

$upDownLoadType = $assembly.GetType("UI.UpDownLoad", $false)
$handler = $upDownLoadType.GetMethod("VerifyUpperBarcodeBackcheck", [System.Reflection.BindingFlags]"Instance, NonPublic")
if ($null -eq $handler) {
    throw "VerifyUpperBarcodeBackcheck is required for upper-camera retry handling."
}

$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\UI\Class\UpDownLoad.cs") -Raw
$lastPositionSection = $source.Substring($source.IndexOf("public EM_RES FlyCamToLastPos"))
if ($lastPositionSection -notmatch "VerifyUpperBarcodeBackcheck\(ref bquit, ws, WsTriPos\[i\]") {
    throw "Last-position feedback must reuse VerifyUpperBarcodeBackcheck."
}

Write-Host "BarcodeBackcheckPolicy contract passed."
