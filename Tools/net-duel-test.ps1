<#
.SYNOPSIS
  End-to-end Card Duel PvP check on localhost (phase 06 v2: every duel stakes both 8-card decks, the winner takes 1 card): one headless dev server (no accounts: the cards move in memory, the API escrow / settle is
  covered by the unit tests and server-api vitest) and two scripted clients (DebugCapture.NetDuel.cs): "Challenger" invites "Rival".
  Scenarios (the server deals from --dev-duel-seed so the result is known):
    win     the challenger wins (seed 4) and takes caveMantis from the rival's deck
    lose    the rival wins (seed 2) and takes caveMantis from the challenger's deck
    draw    a draw (seed 1): nobody loses a card
    drop    the rival quits after 3 placements; the server waits 60 s, forfeits it, the challenger wins and picks
    rejoin  the rival quits after 3 placements and comes back after 12 s: it is put back into the same match
    nopick  the challenger wins and does not pick: the server takes a random card of the rival's deck at 30 s
  Prints every "duel-e2e:" line of both clients and every duel line of the server. Needs a player built from a copy (Tools/build-from-copy.ps1).
.EXAMPLE
  .\Tools\net-duel-test.ps1 -Exe C:\temp\player-cards-p6b\Zenonia.exe -OutDir C:\temp\duel-test -Scenario win
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "duel-test-out",
  [ValidateSet("win", "lose", "draw", "drop", "rejoin", "nopick")][string]$Scenario = "win",
  [int]$Port = 7792,
  [string]$DevKey = "k",
  [int]$TimeoutSeconds = 240
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue
$seed = switch ($Scenario) { "lose" { 2 } "draw" { 1 } default { 4 } }

function Start-Client([string]$name, [string]$class, [string]$role, [string]$log, [string[]]$extra = @()) {
  $args2 = @("-batchmode", "-nographics", "--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", $name, "--class", $class, "--dev-key", $DevKey, "--net-bot", "duel", "--duel-role", $role, "-logFile", "$OutDir\$log.log") + $extra
  return Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList $args2
}

$server = Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", $DevKey, "--dev-level", 60, "--dev-cards", "--dev-duel-seed", $seed, "--map", "oakhaven-village",
  "--run-seconds", ($TimeoutSeconds + 30), "--net-stats", "$OutDir\server.json", "-logFile", "$OutDir\server.log")
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline -and -not ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue))) { Start-Sleep -Milliseconds 300 }
$rivalExtra = @(); if ($Scenario -eq "drop") { $rivalExtra = @("--duel-drop", 3) }; if ($Scenario -eq "rejoin") { $rivalExtra = @("--duel-drop", 3) }
$challengerExtra = @(); if ($Scenario -eq "nopick") { $challengerExtra = @("--duel-nopick") }
if ($Scenario -eq "lose") { $rivalExtra = @("--duel-pick", "caveMantis") }
$rival = Start-Client "Rival" "wizard" "rival" "Rival" $rivalExtra
Start-Sleep -Seconds 2
$challenger = Start-Client "Challenger" "warrior" "challenger" "Challenger" $challengerExtra
$rival2 = $null
$end = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $end) {
  if ($Scenario -eq "rejoin" -and $null -eq $rival2 -and $rival.HasExited) { Start-Sleep -Seconds 12; $rival2 = Start-Client "Rival" "wizard" "rival" "Rival2" @("--duel-resume") }
  $rivalDone = if ($Scenario -eq "rejoin") { ($null -ne $rival2) -and $rival2.HasExited } else { $rival.HasExited }
  if ($challenger.HasExited -and $rivalDone) { break }
  Start-Sleep -Seconds 1
}
Start-Sleep -Seconds 2
foreach ($p in @($challenger, $rival, $rival2, $server)) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
foreach ($n in "Challenger", "Rival", "Rival2") { if (Test-Path "$OutDir\$n.log") { Write-Host "== $n"; Select-String -Path "$OutDir\$n.log" -Pattern "duel-e2e:" | ForEach-Object { $_.Line -replace '^.*duel-e2e: ', '  ' } } }
Write-Host "== server"; Select-String -Path "$OutDir\server.log" -Pattern "\] duel:" | ForEach-Object { $_.Line }
