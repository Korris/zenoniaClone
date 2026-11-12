<#
.SYNOPSIS
  Pictures of the online account pages (login, character list, choose class, customize and name, delete) from a real window, on localhost:
  a throw-away mongod (server-api/scripts/dev-mongo.mjs, no Docker) + the real server-api + the player with --login-shots. No zone server:
  the last step (entering the world) is expected to fail. Secrets are made up for the run. Build the player first (Tools/build-from-copy.ps1).
.EXAMPLE
  .\Tools\character-flow-shots.ps1 -Exe C:\temp\player-char-flow\Zenonia.exe -OutDir C:\temp\char-flow-shots
#>
param([Parameter(Mandatory = $true)][string]$Exe, [string]$OutDir = "char-flow-shots", [int]$ApiPort = 3196, [int]$MongoPort = 27196, [int]$Width = 1138, [int]$Height = 640)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
$apiDir = Join-Path (Split-Path $PSScriptRoot -Parent) "server-api"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue
function New-Secret { -join ((1..48) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) }) }
$password = "pw-" + (New-Secret)
$api = "http://127.0.0.1:$ApiPort"
$mongo = $null; $apiProc = $null; $client = $null
try {
  $mongo = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$OutDir\mongo-data`"") -RedirectStandardOutput "$OutDir\mongo.out" -RedirectStandardError "$OutDir\mongo.err"
  $until = (Get-Date).AddSeconds(90)
  while ((Get-Date) -lt $until -and -not (Select-String -Path "$OutDir\mongo.out" -Pattern "READY" -Quiet -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 300 }
  if (-not (Select-String -Path "$OutDir\mongo.out" -Pattern "READY" -Quiet)) { throw "mongod did not start, see $OutDir\mongo.err" }
  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = "mongodb://127.0.0.1:$MongoPort/?replicaSet=rs0"; MONGO_DB = "char_flow_shots"; JWT_SECRET = (New-Secret); SERVICE_KEY = (New-Secret); REGISTER_IP_MAX = 500; BCRYPT_COST = 4 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $apiProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api.out" -RedirectStandardError "$OutDir\api.err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  $until = (Get-Date).AddSeconds(30); $up = $false
  while ((Get-Date) -lt $until -and -not $up) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; $up = $true } catch { Start-Sleep -Milliseconds 300 } }
  if (-not $up) { throw "server-api did not start, see $OutDir\api.err" }
  $account = "shot" + (Get-Random -Maximum 99999)
  $env:ZENONIA_TEST_PASSWORD = $password
  $client = Start-Process $Exe -PassThru -ArgumentList @("--mode", "client", "-screen-fullscreen", 0, "-screen-width", $Width, "-screen-height", $Height, "-logFile", "`"$OutDir\client.log`"", "--api-url", $api, "--account", $account, "--name", $account, "--login-shots", "`"$OutDir\shots`"")
  Remove-Item env:ZENONIA_TEST_PASSWORD
  if (-not $client.WaitForExit(150000)) { Write-Host "client still running after 150 s: stopping it" }
  Select-String -Path "$OutDir\client.log" -Pattern "persist-e2e: (PASS|FAIL)|NullReference|Exception" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host ($_.Line -replace "^.*persist-e2e: ", "") }
  Write-Host "shots:"; (Get-ChildItem "$OutDir\shots" -Filter *.png -ErrorAction SilentlyContinue).Name
}
finally {
  foreach ($p in @($client, $apiProc, $mongo)) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
  Get-CimInstance Win32_Process -Filter "Name LIKE 'mongod%'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*--port $MongoPort*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
