<#
.SYNOPSIS
  Play the ONLINE game on this PC alone, to try the account pages by hand (register, character list, choose class, customize and name,
  enter the world): a local mongod (server-api/scripts/dev-mongo.mjs, no Docker), the real server-api, a headless zone server and one
  game window pointed at them. Accounts and heroes are kept in -DataDir between runs. Everything stops when the game window is closed.
  Build the player first: .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player-char-flow\Zenonia.exe
  server-api needs its packages once: cd server-api; npm ci
.EXAMPLE
  .\Tools\play-online-local.ps1 -Exe C:\temp\player-char-flow\Zenonia.exe
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$DataDir = "C:\temp\zenonia-local-online",
  [int]$ZonePort = 7796, [int]$ApiPort = 3197, [int]$MongoPort = 27197,
  [int]$Width = 1280, [int]$Height = 720
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
$apiDir = Join-Path (Split-Path $PSScriptRoot -Parent) "server-api"
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
$DataDir = (Resolve-Path $DataDir).Path
function New-Secret { -join ((1..48) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) }) }
# The secrets stay with the data folder: logins made in one run must still be valid in the next.
$secretFile = "$DataDir\secrets.txt"
if (-not (Test-Path $secretFile)) { "$(New-Secret)`n$(New-Secret)" | Set-Content $secretFile -Encoding ascii }
$jwt, $serviceKey = Get-Content $secretFile
$api = "http://127.0.0.1:$ApiPort"
$mongo = $null; $apiProc = $null; $zone = $null; $client = $null
try {
  Remove-Item "$DataDir\mongo.out", "$DataDir\zone.log" -ErrorAction SilentlyContinue
  $mongo = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$DataDir\mongo-data`"") -RedirectStandardOutput "$DataDir\mongo.out" -RedirectStandardError "$DataDir\mongo.err"
  $until = (Get-Date).AddSeconds(90)
  while ((Get-Date) -lt $until -and -not (Select-String -Path "$DataDir\mongo.out" -Pattern "READY" -Quiet -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 300 }
  if (-not (Select-String -Path "$DataDir\mongo.out" -Pattern "READY" -Quiet)) { throw "mongod did not start, see $DataDir\mongo.err" }
  Write-Host "mongod up"

  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = "mongodb://127.0.0.1:$MongoPort/?replicaSet=rs0"; MONGO_DB = "zenonia_local"; JWT_SECRET = $jwt; SERVICE_KEY = $serviceKey; ZONE_ID = "local-zone"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; REGISTER_IP_MAX = 500; LOGIN_IP_MAX = 5000; BCRYPT_COST = 6 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $apiProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$DataDir\api.out" -RedirectStandardError "$DataDir\api.err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  $until = (Get-Date).AddSeconds(30); $up = $false
  while ((Get-Date) -lt $until -and -not $up) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; $up = $true } catch { Start-Sleep -Milliseconds 300 } }
  if (-not $up) { throw "server-api did not start, see $DataDir\api.err (missing packages: cd server-api; npm ci)" }
  Write-Host "server-api up at $api"

  $env:ZENONIA_SERVICE_KEY = $serviceKey
  $zone = Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "local-zone", "--save-interval", 5, "--save-spool", "`"$DataDir\spool`"", "-logFile", "`"$DataDir\zone.log`"")
  Remove-Item env:ZENONIA_SERVICE_KEY
  $until = (Get-Date).AddSeconds(60)
  $ready = $false   # the log file appears a moment after the process starts
  while ((Get-Date) -lt $until -and -not $ready) { Start-Sleep -Milliseconds 500; $ready = (Test-Path "$DataDir\zone.log") -and [bool](Select-String -Path "$DataDir\zone.log" -Pattern "zone sessions reset" -Quiet) }
  if (-not $ready) { throw "the zone server never got ready, see $DataDir\zone.log" }
  Write-Host "zone server up on port $ZonePort"

  Write-Host "Opening the game. Register an account on the first page (any email like a@b.cc works here: no mail is sent). Close the window to stop everything."
  $client = Start-Process $Exe -PassThru -ArgumentList @("--mode", "client", "-screen-fullscreen", 0, "-screen-width", $Width, "-screen-height", $Height, "--api-url", $api, "-logFile", "`"$DataDir\client.log`"")
  $client.WaitForExit()
}
finally {
  foreach ($p in @($client, $zone, $apiProc, $mongo)) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
  Get-CimInstance Win32_Process -Filter "Name LIKE 'mongod%'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*--port $MongoPort*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  Write-Host "stopped"
}
