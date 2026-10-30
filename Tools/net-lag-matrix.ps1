<#
.SYNOPSIS
  Lag / loss matrix of the online client: for every (RTT, loss) pair runs Tools/net-local-test.ps1 (one headless server + N headless bot clients, all
  -batchmode -nographics, no windows) with simulated latency / jitter / loss and collects the numbers that matter for prediction:
  mean / p99 / max correction of the predicted hero, input-to-motion latency, interpolation delay, bandwidth per client, input frames the server cut.

  RTT is the round trip: each direction gets RTT/2 (+- 10 % jitter). Loss is per direction (so a round trip loses roughly twice as much).

.EXAMPLE
  .\Tools\net-lag-matrix.ps1 -Exe C:\path\Zenonia.exe -OutDir C:\temp\lag
  .\Tools\net-lag-matrix.ps1 -Exe ... -Rtt 100,250 -Loss 2,5 -Clients 8 -Seconds 45 -NoPredictToo     # also the same scenarios without prediction
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [int[]]$Rtt = @(0, 80, 150, 250),
  [int[]]$Loss = @(0, 2, 5, 10),
  [int]$Clients = 8,
  [int]$Seconds = 45,
  [int]$TwitchBots = 2,
  [string]$OutDir = "net-lag-out",
  [int]$Port = 7810,
  [switch]$NoPredictToo,
  [switch]$NoPredictOnly
)
$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$test = Join-Path $PSScriptRoot "net-local-test.ps1"
$checker = Join-Path $PSScriptRoot "net-check-summary.mjs"
$rows = @()
$port = $Port

function Run-One([int]$rtt, [int]$loss, [bool]$predict) {
  $script:port++
  $tag = "rtt{0}-loss{1}{2}" -f $rtt, $loss, $(if ($predict) { "" } else { "-nopredict" })
  $dir = Join-Path $OutDir $tag
  $oneWay = [int]($rtt / 2)
  $jitter = [int][math]::Round($oneWay * 0.1)
  $extra = @(); if (-not $predict) { $extra = @("--no-predict") }
  $tol = 250 + 2 * $rtt
  & $test -Exe $Exe -Clients $Clients -Seconds $Seconds -OutDir $dir -Port $script:port -SimLatency $oneWay -SimJitter $jitter -SimLoss $loss -TwitchBots $TwitchBots -ClientArgs $extra -ToleranceMs $tol -NoCheck | Out-Null
  node $checker $dir --clients $Clients --tolerance-ms $tol --lag | Out-File (Join-Path $dir "check.txt")
  $m = (Get-Content (Join-Path $dir "summary.json") -Raw | ConvertFrom-Json).metrics
  $row = [pscustomobject]@{
    RttMs = $rtt; LossPct = $loss; Predict = $predict
    CorrMean = [math]::Round([double]$m.correctionMean, 3); CorrP99 = [math]::Round([double]$m.correctionP99, 2); CorrMax = [math]::Round([double]$m.correctionMax, 1); RubberMax = [math]::Round([double]$m.rubberMax, 1); Teleports = $m.teleportCount
    Corrected = $(if ($m.reconciles) { "{0:P1}" -f ([double]$m.reconcilesCorrected / [double]$m.reconciles) } else { "-" })
    InputToMotionMs = $(if ($m.inputToMotionMsAvg) { [math]::Round([double]$m.inputToMotionMsAvg, 0) } else { $null })
    InterpDelayMs = $(if ($m.interpDelayMsAvg) { [math]::Round([double]$m.interpDelayMsAvg, 0) } else { $null })
    JitterMs = $(if ($m.arrivalJitterMsAvg) { [math]::Round([double]$m.arrivalJitterMsAvg, 1) } else { $null })
    DownKBps = [math]::Round([double]$m.downKBpsAvg, 2); UpKBps = [math]::Round([double]$m.upKBpsAvg, 2)
    InputCut = $m.inputFramesDropped; InputFrozen = $m.inputStepsStarved; CatchUp = $m.inputCatchUps; ChecksFailed = $m.failed
  }
  Write-Host ($row | Format-Table -HideTableHeaders -AutoSize | Out-String).Trim()
  return $row
}

foreach ($r in $Rtt) {
  foreach ($l in $Loss) {
    if (-not $NoPredictOnly) { $rows += Run-One $r $l $true }
    if ($NoPredictToo -or $NoPredictOnly) { $rows += Run-One $r $l $false }
  }
}
$rows | ConvertTo-Json | Out-File (Join-Path $OutDir "lag-matrix.json")
$md = "| RTT ms | loss % each way | predict | corr mean (units) | corr p99 | corr max | rubber band max | teleports | corrected | input->motion ms | interp delay ms | jitter ms | down KB/s | up KB/s | input cut | input frozen | catch-up steps |`n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|`n"
foreach ($x in $rows) { $md += "| $($x.RttMs) | $($x.LossPct) | $($x.Predict) | $($x.CorrMean) | $($x.CorrP99) | $($x.CorrMax) | $($x.RubberMax) | $($x.Teleports) | $($x.Corrected) | $($x.InputToMotionMs) | $($x.InterpDelayMs) | $($x.JitterMs) | $($x.DownKBps) | $($x.UpKBps) | $($x.InputCut) | $($x.InputFrozen) | $($x.CatchUp) |`n" }
$md | Out-File (Join-Path $OutDir "lag-matrix.md") -Encoding utf8
Write-Host $md
