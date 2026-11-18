param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot "..\UI\bin\x64\Pre-Mini.exe")
)

$assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))
$type = $assembly.GetType("UI.FeedbackBatch", $false)
if ($null -eq $type) {
    throw "FeedbackBatch type is required to retain unfinished feedback modules after a stop."
}

$batch = [Activator]::CreateInstance($type)
$beginArgs = New-Object object[] 1
$beginArgs[0] = [int[]](1, 5)
$type.GetMethod("Begin").Invoke($batch, $beginArgs)

if (-not $type.GetMethod("IsPending").Invoke($batch, @(1))) {
    throw "Module 1 should be pending after Begin."
}
if (-not $type.GetMethod("IsPending").Invoke($batch, @(5))) {
    throw "Module 5 should be pending after Begin."
}

$type.GetMethod("Complete").Invoke($batch, @(1))
if ($type.GetMethod("IsPending").Invoke($batch, @(1))) {
    throw "Completed module 1 must not be checked again after restart."
}
if (-not $type.GetMethod("IsPending").Invoke($batch, @(5))) {
    throw "Unfinished module 5 must remain pending after restart."
}

$blockedArgs = New-Object object[] 1
$blockedArgs[0] = [int[]](2, 6)
$wasStarted = $type.GetMethod("TryBegin").Invoke($batch, $blockedArgs)
if ($wasStarted) {
    throw "A pending feedback batch must block a replacement batch."
}
if (-not $type.GetMethod("IsPending").Invoke($batch, @(5))) {
    throw "The original pending module must remain pending when a later batch is rejected."
}

$type.GetMethod("Complete").Invoke($batch, @(5))
if ($type.GetProperty("HasPending").GetValue($batch)) {
    throw "Batch should be empty after every module is completed."
}

Write-Host "FeedbackBatch contract passed."
