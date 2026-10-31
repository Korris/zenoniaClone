<#
.SYNOPSIS
  End-to-end trade check on localhost: one headless server with goods to trade (--dev-gold, --dev-items) and scripted clients (DebugCapture.NetTrade.cs). Default: Ari and Bea do the honest
  flow, Cyd pokes at it, Eve is a malicious client. -Ui: Ari and Bea are visible windows that use the real buttons through FakeInput and save PNGs of every state into OutDir.
  Prints every "trade-e2e:" line, every trade line of the server, and how many checks failed. Needs a player built from a copy (Tools/build-from-copy.ps1).
.EXAMPLE
  .\Tools\net-trade-test.ps1 -Exe C:\temp\player\Zenonia.exe -OutDir C:\temp\trade -Ui
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "trade-test-out",
  [int]$Port = 7792,
  [string]$DevKey = "k",
  [int]$TimeoutSeconds = 300,
  [int]$Width = 960,                 # window size with -Ui: the layout must also hold on a wide phone and on a 4:3 tablet
  [int]$Height = 640,
  [switch]$Ui
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function Start-Client([string]$name, [string]$class, [string]$role, [bool]$visible) {
  $common = @("--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", $name, "--class", $class, "--dev-key", $DevKey, "--net-bot", "trade", "--trade-role", $role, "-logFile", "$OutDir\$name.log")
  if ($visible) { return Start-Process -FilePath $Exe -PassThru -ArgumentList (@("-screen-width", $Width, "-screen-height", $Height, "-screen-fullscreen", 0, "--shot-path", $OutDir) + $common) }
  return Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList (@("-batchmode", "-nographics") + $common)
}

$server = Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", $DevKey, "--dev-level", 60, "--dev-gold", 5000, "--dev-items", 10,
  "--run-seconds", ($TimeoutSeconds + 30), "--net-stats", "$OutDir\server.json", "-logFile", "$OutDir\server.log")
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline -and -not ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue))) { Start-Sleep -Milliseconds 300 }

$clients = @()
if ($Ui) {
  $names = "Ari", "Bea"
  $clients += Start-Client "Ari" "warrior" "ui-a" $true
  Start-Sleep -Seconds 2
  $clients += Start-Client "Bea" "wizard" "ui-b" $true
} else {
  $names = "Ari", "Bea", "Cyd", "Eve"
  $clients += Start-Client "Ari" "warrior" "ari" $false
  Start-Sleep -Seconds 2
  $clients += Start-Client "Bea" "wizard" "bea" $false
  Start-Sleep -Seconds 1
  $clients += Start-Client "Cyd" "mechanic" "cyd" $false
  Start-Sleep -Seconds 1
  $clients += Start-Client "Eve" "assassin" "eve" $false
}

$until = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $until -and @($clients | Where-Object { -not $_.HasExited }).Count -gt 0) { Start-Sleep -Seconds 1 }
foreach ($p in $clients + @($server)) { if (-not $p.HasExited) { $p.Kill() } }

$failed = 0; $passed = 0
foreach ($n in $names) {
  Write-Host "--- $n"
  Select-String -Path "$OutDir\$n.log" -Pattern "trade-e2e:|\] trade:|MISSING on screen|NullReference|Exception" | ForEach-Object {
    $line = $_.Line -replace "^\[ZenoniaProto\] ", ""
    if ($line -match "trade-e2e: FAIL|MISSING on screen|NullReference|Exception") { $failed++ }
    if ($line -match "trade-e2e: PASS") { $passed++ }
    Write-Host $line
  }
}
Write-Host "--- server"
Select-String -Path "$OutDir\server.log" -Pattern "\] trade:| joined,| left \(|kicked|NullReference|Exception" | ForEach-Object { $_.Line -replace "^\[ZenoniaProto\] ", "" }
Write-Host "--- $passed passed, $failed failed"
if ($failed -gt 0) { exit 1 }
