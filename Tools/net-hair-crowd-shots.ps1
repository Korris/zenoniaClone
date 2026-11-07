<#
.SYNOPSIS
  Crowd look capture: a local zone server, N protocol bots (Tests/dotnet/ZenoniaSwarm) that only walk around (no attacks), each class with
  all 5 hairstyles (--hair vary), and one visible client that stands in the crowd and takes screenshots. For judging whether players tell
  each other apart (hairstyles, weapons) at game scale.
.EXAMPLE
  .\Tools\net-hair-crowd-shots.ps1 -Exe C:\temp\player-hair\Zenonia.exe -OutDir C:\temp\crowd -Bots 50
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "C:\temp\crowd",
  [int]$Bots = 50,
  [int]$Port = 7810,
  [string]$Class = "warrior",          # the camera hero
  [string]$Shots = "12,16,20,24,28",  # seconds after the viewer starts
  [int]$Zoom = 100                     # camera half-height (default game view 80)
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
$repo = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -ErrorAction SilentlyContinue
$swarmDir = Join-Path $repo "Tests\dotnet\ZenoniaSwarm"
if (-not ((dotnet --list-sdks) -match '^1\d\.')) {
  # this machine may only have the .NET 9 SDK (the csproj targets net10.0): build a net9.0 copy of the project from a temp folder
  $copy = Join-Path $env:TEMP "zenonia-swarm-net9"
  New-Item -ItemType Directory -Force -Path $copy | Out-Null
  (Get-Content "$swarmDir\ZenoniaSwarm.csproj" -Raw).Replace("net10.0", "net9.0").Replace('"*.cs"', "`"$swarmDir\*.cs`"").Replace('..\..\..\', "$repo\") |
    Set-Content "$copy\ZenoniaSwarm.csproj" -Encoding utf8
  $swarmDir = $copy
}
dotnet build "$swarmDir\ZenoniaSwarm.csproj" -c Release --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "swarm build failed" }
$swarmDll = Get-ChildItem (Join-Path $swarmDir "bin\Release") -Recurse -Filter "ZenoniaSwarm.dll" | Select-Object -First 1

$server = Start-Process -FilePath $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $Port,
  "--dev-key", "k", "--max-players", ($Bots + 5), "--run-seconds", 120, "--no-portals", "-logFile", "$OutDir\server.log")
for ($i = 0; $i -lt 60; $i++) {
  Start-Sleep -Milliseconds 500
  if ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue)) { break }
}
$swarm = Start-Process -FilePath "dotnet" -PassThru -WindowStyle Hidden -RedirectStandardOutput "$OutDir\swarm.txt" -ArgumentList @($swarmDll.FullName,
  "--host", "127.0.0.1", "--port", $Port, "--key", "k", "--clients", $Bots, "--seconds", 90, "--stagger-ms", 40, "--mix", "0:100:0", "--no-attack", "--hair", "vary")
Start-Sleep -Seconds 6
$viewer = Start-Process -FilePath $Exe -PassThru -ArgumentList @("-screen-width", 1280, "-screen-height", 720, "-screen-fullscreen", 0,
  "--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", "Viewer", "--class", $Class, "--dev-key", "k",
  "--zoom", $Zoom, "--shots", $Shots, "--shot-path", "$OutDir\crowd", "-logFile", "$OutDir\viewer.log")
if (-not $viewer.WaitForExit(90000)) { $viewer.Kill() }
foreach ($p in @($swarm, $server)) { if (-not $p.HasExited) { $p.Kill() } }
Get-ChildItem "$OutDir\crowd_*.png" | ForEach-Object { Write-Host $_.FullName }
