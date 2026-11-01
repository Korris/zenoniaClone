<#
.SYNOPSIS
  Real login and saving on this machine: a local mongod (server-api\.dev-data, kept between runs, Docker not needed), server-api, a headless zone server with --api-url and one game window on the
  login screen. Accounts and characters stay until server-api\.dev-data is deleted. The secrets are made up per machine run and only live in the started processes, so LOCAL USE ONLY.
  Close the game windows to stop everything; the zone saves everybody first. Tools\play-local.ps1 stays the dev path: no accounts, nothing saved, cheats on.
.EXAMPLE
  .\Tools\play-online.ps1 -Exe C:\temp\login-build\Zenonia.exe -Windows 2
#>
param(
  [string]$Exe = "",
  [int]$Windows = 1,
  [int]$ZonePort = 7777,
  [int]$ApiPort = 3000,
  [int]$MongoPort = 27117,
  [string]$DataDir = "",   # default server-api\.dev-data, which keeps the accounts between runs
  [switch]$NoRemember      # this window does not read or write the login remembered on this machine
)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$apiDir = Join-Path $repo "server-api"
if ($Exe -eq "") {
  $found = Get-ChildItem "C:\temp\*\Zenonia.exe" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if ($null -eq $found) { throw "No C:\temp\*\Zenonia.exe. Build one: .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\login-build\Zenonia.exe" }
  $Exe = $found.FullName
}
if (-not (Test-Path (Join-Path $apiDir "node_modules"))) { Push-Location $apiDir; npm install | Out-Null; Pop-Location }
$dir = Split-Path $Exe -Parent
$logs = Join-Path $dir "online-logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
function New-Secret { -join ((1..48) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) }) }

# JWT secret stays per data folder, so a remembered login survives a restart of this script. Never committed: .dev-data is ignored.
$dataDir = if ($DataDir -ne "") { $DataDir } else { Join-Path $apiDir ".dev-data" }
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$secretFile = Join-Path $dataDir "dev-jwt-secret.txt"
if (-not (Test-Path $secretFile)) { New-Secret | Set-Content $secretFile }
$jwt = (Get-Content $secretFile -TotalCount 1).Trim()
$serviceKey = New-Secret
$api = "http://127.0.0.1:$ApiPort"
$stop = Join-Path $logs "stop-zone"
# Something else on these ports is not ours to stop: say so and let the person choose other ports.
foreach ($port in @($ApiPort, $MongoPort)) { if (@(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue).Count -gt 0) { throw "TCP port $port is in use by another program. Close it or pass another port (-ApiPort, -MongoPort)." } }
if (@(Get-NetUDPEndpoint -LocalPort $ZonePort -ErrorAction SilentlyContinue).Count -gt 0) { throw "UDP port $ZonePort is in use by another program. Close it or pass another port (-ZonePort)." }
Remove-Item $stop -ErrorAction SilentlyContinue

$mongo = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$dataDir\mongo`"") -RedirectStandardOutput "$logs\mongo.out" -RedirectStandardError "$logs\mongo.err"
$until = (Get-Date).AddSeconds(60)
while ((Get-Date) -lt $until -and -not (Select-String -Path "$logs\mongo.out" -Pattern "READY" -Quiet -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 300 }
if (-not (Select-String -Path "$logs\mongo.out" -Pattern "READY" -Quiet)) { throw "mongod did not start, see $logs\mongo.err" }

$vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = "mongodb://127.0.0.1:$MongoPort/?replicaSet=rs0"; MONGO_DB = "zenonia"; JWT_SECRET = $jwt; SERVICE_KEY = $serviceKey; ZONE_ID = "frost-hollow"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort }
foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
$apiProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$logs\api.out" -RedirectStandardError "$logs\api.err"
foreach ($k in $vars.Keys) { Remove-Item "env:$k" }

$env:ZENONIA_SERVICE_KEY = $serviceKey
$zone = Start-Process $Exe -WorkingDirectory $dir -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "-logFile", "`"$logs\zone.log`"", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--shutdown-file", "`"$stop`"", "--save-spool", "`"$logs\save-failed`"")
Remove-Item env:ZENONIA_SERVICE_KEY
Start-Sleep -Seconds 3

$clients = @()
for ($i = 1; $i -le $Windows; $i++) {
  $remember = if ($Windows -gt 1 -or $NoRemember) { "--no-remember" } else { "--remember" } # one machine has one remembered login: several windows would all be the same account
  $clients += Start-Process $Exe -WorkingDirectory $dir -PassThru -ArgumentList @($remember, "--mode", "client", "--api-url", $api, "-screen-fullscreen", 0, "-screen-width", 1138, "-screen-height", 640, "-logFile", "`"$logs\client-$i.log`"")
}
Write-Host "Playing with $Windows window(s): log in or create an account. API $api, logs in $logs. Close the windows to stop."
$clients | Wait-Process
New-Item -ItemType File -Path $stop | Out-Null
if (-not $zone.WaitForExit(40000)) { Stop-Process -Id $zone.Id -Force -ErrorAction SilentlyContinue; Write-Host "The zone did not stop by itself." }
Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $mongo.Id -Force -ErrorAction SilentlyContinue
Get-CimInstance Win32_Process -Filter "Name LIKE 'mongod%'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*--port*$MongoPort*" -and $_.CommandLine -like "*$dataDir*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Write-Host "Stopped."
