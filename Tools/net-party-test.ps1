<#
.SYNOPSIS
  End-to-end party check on localhost: one headless server and three scripted clients (DebugCapture.NetParty.cs): Leader (cleric), Allie, Stranger. Prints every "party-e2e:" line and
  every party line of the server. Needs a player built from a copy (Tools/build-from-copy.ps1). -Shots makes Leader and Allie visible windows that save PNGs of the party UI into OutDir.
.EXAMPLE
  .\Tools\net-party-test.ps1 -Exe C:\temp\player\Zenonia.exe -OutDir C:\temp\party
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "party-test-out",
  [int]$Port = 7791,
  [string]$DevKey = "k",
  [int]$TimeoutSeconds = 420,
  [switch]$Shots,
  [switch]$Fight,                     # last leg: both fight side by side (EXP split between two) instead of the ally leaving through a portal
  [switch]$Ui                         # the UI through its real input path (DebugCapture.NetPartyUi.cs): Leader and Allie are visible windows, three headless joiners fill the party, PNGs in OutDir
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function Start-Client([string]$name, [string]$class, [string]$role, [bool]$visible) {
  $common = @("--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", $name, "--class", $class, "--dev-key", $DevKey, "--net-bot", "party", "--party-role", $role, "-logFile", "$OutDir\$name.log")
  if ($Fight) { $common += "--party-fight" }
  if ($visible) { return Start-Process -FilePath $Exe -PassThru -ArgumentList (@("-screen-width", 960, "-screen-height", 640, "-screen-fullscreen", 0, "--party-shots", "--shot-path", $OutDir) + $common) }
  return Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList (@("-batchmode", "-nographics") + $common)
}

$server = Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", $DevKey, "--dev-level", 60, "--dev-hurt", 20,
  "--run-seconds", ($TimeoutSeconds + 30), "--net-stats", "$OutDir\server.json", "-logFile", "$OutDir\server.log")
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline -and -not ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue))) { Start-Sleep -Milliseconds 300 }

$others = @()
if ($Ui) {
  $leader = Start-Client "Leader" "cleric" "ui-leader" $true
  Start-Sleep -Seconds 2
  $others += Start-Client "Allie" "warrior" "ui-ally" $true
  Start-Sleep -Seconds 2
  $others += Start-Client "Luna" "wizard" "joiner" $false
  $others += Start-Client "Kirito-the-Long1" "mechanic" "joiner" $false
  $others += Start-Client "Sora" "assassin" "joiner-away" $false
} else {
  $leader = Start-Client "Leader" "cleric" "leader" $Shots
  Start-Sleep -Seconds 2
  $others += Start-Client "Allie" "warrior" "ally" $Shots
  Start-Sleep -Seconds 2
  $others += Start-Client "Stranger" "wizard" "stranger" $false
}

if (-not $leader.WaitForExit($TimeoutSeconds * 1000)) { Write-Warning "the leader did not finish in time" }
if ($Ui) { Start-Sleep -Seconds 8 } # Allie finishes a little later
foreach ($p in @($leader) + $others + @($server)) { if (-not $p.HasExited) { $p.Kill() } }

foreach ($n in $(if ($Ui) { "Leader", "Allie", "Sora" } else { "Leader", "Allie", "Stranger" })) {
  Write-Host "--- $n"
  Select-String -Path "$OutDir\$n.log" -Pattern "party-e2e:|\] party:" | ForEach-Object { $_.Line -replace "^\[ZenoniaProto\] ", "" }
}
Write-Host "--- server"
Select-String -Path "$OutDir\server.log" -Pattern "\] party:| joined,| left \(| -> " | ForEach-Object { $_.Line -replace "^\[ZenoniaProto\] ", "" }
