<#
.SYNOPSIS
  End-to-end Card Duel PvP check on localhost (phase 06): one headless dev server (no accounts: the wager moves in memory, the API escrow is covered by the unit tests) and two scripted clients
  (DebugCapture.NetDuel.cs): "Challenger" invites "Rival". Scenarios: plain (no wager), wager (both put in meadowSlime), drop (wager; Rival quits after 3 placements, the server waits 60 s and forfeits it).
  Prints every "duel-e2e:" line of both clients and every duel line of the server. Needs a player built from a copy (Tools/build-from-copy.ps1).
.EXAMPLE
  .\Tools\net-duel-test.ps1 -Exe C:\temp\player-cards-p6\Zenonia.exe -OutDir C:\temp\duel-test -Scenario wager
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "duel-test-out",
  [ValidateSet("plain", "wager", "drop")][string]$Scenario = "plain",
  [int]$Port = 7792,
  [string]$DevKey = "k",
  [int]$TimeoutSeconds = 240
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function Start-Client([string]$name, [string]$class, [string]$role) {
  $args2 = @("-batchmode", "-nographics", "--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", $name, "--class", $class, "--dev-key", $DevKey, "--net-bot", "duel", "--duel-role", $role, "-logFile", "$OutDir\$name.log")
  if ($Scenario -ne "plain") { $args2 += "--duel-wager" }
  if ($Scenario -eq "drop" -and $role -eq "rival") { $args2 += @("--duel-drop", 3) }
  return Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList $args2
}

$server = Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", $DevKey, "--dev-level", 60, "--dev-cards", "--map", "oakhaven-village",
  "--run-seconds", ($TimeoutSeconds + 30), "--net-stats", "$OutDir\server.json", "-logFile", "$OutDir\server.log")
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline -and -not ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue))) { Start-Sleep -Milliseconds 300 }
$rival = Start-Client "Rival" "wizard" "rival"
Start-Sleep -Seconds 2
$challenger = Start-Client "Challenger" "warrior" "challenger"
$end = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $end -and -not ($challenger.HasExited -and ($rival.HasExited))) { Start-Sleep -Seconds 1 }
Start-Sleep -Seconds 2
foreach ($p in @($challenger, $rival, $server)) { if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
foreach ($n in "Challenger", "Rival") { Write-Host "== $n"; Select-String -Path "$OutDir\$n.log" -Pattern "duel-e2e:" | ForEach-Object { $_.Line -replace '^.*duel-e2e: ', '  ' } }
Write-Host "== server"; Select-String -Path "$OutDir\server.log" -Pattern "\] duel:" | ForEach-Object { $_.Line }
