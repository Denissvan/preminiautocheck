$sourcePath = Join-Path $PSScriptRoot "..\UI\Class\UpDownLoad.cs"
$source = Get-Content -LiteralPath $sourcePath -Raw
$methodStart = $source.IndexOf("public EM_RES UpDownLoadModleAct(ref bool bquit, WSFEED wsfeed)")
$gate = $source.IndexOf("RecoverPendingFeedbackBeforeNextAction(ref bquit, wsfeed)", $methodStart)
$nextAction = $source.IndexOf("Proc = ChkNextProc", $methodStart, [System.StringComparison]::Ordinal)

if ($gate -lt 0) {
    throw "Recovery feedback gate is required."
}
if ($nextAction -lt 0 -or $gate -gt $nextAction) {
    throw "Pending feedback must be recovered before selecting the next pick/place action."
}
if ($source -notmatch "if \(feedbackBatch\.HasPending\)") {
    throw "Recovery feedback gate must only run for unfinished feedback."
}

$guardedCalls = [regex]::Matches($source, "if \(!BeginFeedbackBatch\(WsTriPos\)\) return EM_RES\.RETRY;")
if ($guardedCalls.Count -ne 2) {
    throw "Both FlyToWs entry points must reject a new placement while feedback is pending."
}

Write-Host "Feedback recovery gate contract passed."
