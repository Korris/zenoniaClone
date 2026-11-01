<#
.SYNOPSIS
  Local multiplayer test without typing commands: starts a hidden local server and N game windows, and stops the server when the last window closes.
  Double-click Tools\play-local.cmd, or run it with arguments. Uses the newest C:\temp\*\Zenonia.exe unless -Exe is given.
  DEV ONLY: shared key, no accounts, nothing is saved, and the server runs with dev cheats (level, gold, items). For real login and saving: Tools\play-online.ps1.
.EXAMPLE
  .\Tools\play-local.ps1 -Classes warrior,cleric,assassin
#>
param(
  [string]$Exe = "",
  [string[]]$Classes = @("warrior", "cleric"),
  [int]$Port = 7777,
  [int]$Level = 60,
  [int]$Gold = 5000,
  [int]$Items = 10
)
$ErrorActionPreference = "Stop"

if ($Exe -eq "") {
  $found = Get-ChildItem "C:\temp\*\Zenonia.exe" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if ($null -eq $found) { throw "No C:\temp\*\Zenonia.exe. Build one: .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player\Zenonia.exe" }
  $Exe = $found.FullName
}
Write-Host "Game: $Exe"

# Leftovers of an earlier run on this port would hold it and may speak an older protocol.
Get-CimInstance Win32_Process -Filter "Name='Zenonia.exe'" |
  Where-Object { $_.CommandLine -like "*--port $Port*" } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

$dir = Split-Path $Exe -Parent
$serverLog = Join-Path $dir "local-server.log"
$server = Start-Process $Exe -WorkingDirectory $dir -PassThru -ArgumentList "-batchmode -nographics -logFile `"$serverLog`" --mode server --port $Port --dev-key k --dev-level $Level --dev-gold $Gold --dev-items $Items"
Start-Sleep -Seconds 3
if ($server.HasExited) { throw "The server stopped at once, see $serverLog" }

$names = "A", "B", "C", "D", "E"
$clients = @()
for ($i = 0; $i -lt $Classes.Count -and $i -lt $names.Count; $i++) {
  $clients += Start-Process $Exe -WorkingDirectory $dir -PassThru -ArgumentList "--mode client --host 127.0.0.1 --port $Port --dev-key k --name $($names[$i]) --class $($Classes[$i]) -screen-fullscreen 0 -screen-width 1138 -screen-height 640 -logFile `"$(Join-Path $dir "local-client-$($names[$i]).log")`""
}
Write-Host "Playing with $($clients.Count) windows. Close them all to stop the server."
$clients | Wait-Process
Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
Write-Host "Server stopped."
