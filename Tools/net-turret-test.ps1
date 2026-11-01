<#
.SYNOPSIS
  End-to-end Deploy Turret check on localhost: a headless server, a Mechanic that places turrets, and a Warrior started only after the first turret stands (DebugCapture.NetTurret.cs). Both are
  visible windows that save PNGs into OutDir. Prints every "turret-e2e:" line and any exception. Needs a player built from a copy (Tools/build-from-copy.ps1).
.EXAMPLE
  .\Tools\net-turret-test.ps1 -Exe C:\temp\turret-build\Zenonia.exe -OutDir C:\temp\turret-run
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "turret-test-out",
  [int]$Port = 7793,
  [string]$DevKey = "k",
  [int]$TimeoutSeconds = 300
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function Start-Client([string]$name, [string]$class, [string]$role) {
  return Start-Process -FilePath $Exe -PassThru -ArgumentList @("-screen-width", 960, "-screen-height", 640, "-screen-fullscreen", 0, "--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", $name, "--class", $class, "--dev-no-cooldowns",
    "--dev-key", $DevKey, "--net-bot", "turret", "--turret-role", $role, "--turret-shots", "--shot-path", $OutDir, "-logFile", "$OutDir\$name.log")
}

$server = Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", $DevKey, "--dev-level", 33, "--dev-no-cooldowns",
  "--run-seconds", ($TimeoutSeconds + 30), "--net-stats", "$OutDir\server.json", "-logFile", "$OutDir\server.log")
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline -and -not ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue))) { Start-Sleep -Milliseconds 300 }

$caster = Start-Client "Allie" "mechanic" "caster"
$deadline = (Get-Date).AddSeconds(150)
while ((Get-Date) -lt $deadline -and -not (Test-Path "$OutDir\turret-spot.txt") -and -not $caster.HasExited) { Start-Sleep -Milliseconds 200 }
$watcher = Start-Client "Watcher" "warrior" "watcher"

if (-not $caster.WaitForExit($TimeoutSeconds * 1000)) { Write-Warning "the caster did not finish in time" }
Start-Sleep -Seconds 6
foreach ($p in @($caster, $watcher, $server)) { if (-not $p.HasExited) { $p.Kill() } }

foreach ($n in "Allie", "Watcher", "server") {
  Write-Host "--- $n"
  Select-String -Path "$OutDir\$n.log" -Pattern "turret-e2e:|NullReference|Exception|\] turret " | ForEach-Object { $_.Line -replace "^\[ZenoniaProto\] ", "" }
}
