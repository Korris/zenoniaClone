<#
.SYNOPSIS
  Hands-on test of the Wanted Boss skills on localhost: the same stack as wanted-boss-test.ps1 (dev mongod, server-api, a headless zone pinned to one
  wanted boss) plus ONE real client window, already logged in as a fresh level-60 hero standing three tiles south of the boss. The window uses the
  "idle" persist role: the bot only logs in, the keyboard / touch controls are yours. Close the game window to stop everything.
  Secrets and the account are made up for this run and die with it (the database lives in OutDir).
.EXAMPLE
  .\Tools\wanted-boss-playtest.ps1 -Exe C:\temp\player-wanted\Zenonia.exe
  .\Tools\wanted-boss-playtest.ps1 -Exe C:\temp\player-wanted\Zenonia.exe -Class wizard -HpScale 1
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "C:\temp\wanted-play",
  [string]$Boss = "rimeclawTyrant",   # rimeclawTyrant | glacialColossus | mireHydraQueen
  [string]$Class = "warrior",
  [double]$HpScale = 0.3,     # 1 = the real event HP (34800 for the Tyrant); 0.3 leaves time to see every skill, the enrage below 30 % included
  [int]$Level = 60,
  [int]$ZonePort = 7798,
  [int]$ApiPort = 3198,
  [int]$MongoPort = 27198,
  [switch]$Bot                # the hero fights on its own (wanted-kill bot) and saves timed screenshots of the first casts into OutDir\shots
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
$repo = Split-Path $PSScriptRoot -Parent
$apiDir = Join-Path $repo "server-api"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function New-Secret { -join ((1..48) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) }) }
$serviceKey = New-Secret; $jwtSecret = New-Secret; $password = "pw-" + (New-Secret)
$api = "http://127.0.0.1:$ApiPort"
$mongoUri = "mongodb://127.0.0.1:$MongoPort/?replicaSet=rs0"
$dbName = "wanted_play"
# Spawn tiles of WantedBoss.cs; the hero starts three tiles south of the boss (map rows x 16 units, y counted from the bottom)
$starts = @{
  rimeclawTyrant = @("--map", "frostHollow", "--x", "1032", "--y", "248")       # tile (64,29), 48 rows
  glacialColossus = @("--map", "frostfangDen", "--x", "440", "--y", "136")      # boss tile (27,12), 24 rows
  mireHydraQueen = @("--map", "sunkenReliquary", "--x", "488", "--y", "104")    # boss tile (30,14), 24 rows
}
if (-not $starts.ContainsKey($Boss)) { throw "no start position for $Boss" }
$procs = @()

function Wait-Log([string]$path, [string]$pattern, [int]$seconds) {
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) {
    if ((Test-Path $path) -and (Select-String -Path $path -Pattern $pattern -Quiet -ErrorAction SilentlyContinue)) { return $true }
    Start-Sleep -Milliseconds 300
  }
  return $false
}
function Api-Call([string]$method, [string]$path, $body, [string]$token = $null) {
  $h = @{}; if ($token) { $h.Authorization = "Bearer $token" }
  $r = Invoke-WebRequest "$api$path" -Method $method -Headers $h -Body ($body | ConvertTo-Json -Depth 6) -ContentType "application/json" -UseBasicParsing -TimeoutSec 8
  return ($r.Content | ConvertFrom-Json)
}

try {
  $procs += Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$OutDir\mongo-data`"") -RedirectStandardOutput "$OutDir\mongo.out" -RedirectStandardError "$OutDir\mongo.err"
  if (-not (Wait-Log "$OutDir\mongo.out" "READY" 60)) { throw "mongod did not start, see $OutDir\mongo.err" }
  Write-Host "mongod up"

  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = $mongoUri; MONGO_DB = $dbName; JWT_SECRET = $jwtSecret; SERVICE_KEY = $serviceKey; ZONE_ID = "wanted-play"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; ACCESS_TTL_SEC = 3600; BCRYPT_COST = 6 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $procs += Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api.out" -RedirectStandardError "$OutDir\api.err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  $until = (Get-Date).AddSeconds(20); $up = $false
  while (-not $up -and (Get-Date) -lt $until) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; $up = $true } catch { Start-Sleep -Milliseconds 300 } }
  if (-not $up) { throw "server-api did not start, see $OutDir\api.err" }
  Write-Host "server-api up"

  $env:ZENONIA_SERVICE_KEY = $serviceKey
  $procs += Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "wanted-play", "--wanted-boss", $Boss, "--wanted-respawn-sec", 20, "--wanted-hp-scale", $HpScale, "-logFile", "`"$OutDir\zone.log`"")
  Remove-Item env:ZENONIA_SERVICE_KEY
  if (-not (Wait-Log "$OutDir\zone.log" "zone sessions reset" 40)) { throw "the zone never got ready, see $OutDir\zone.log" }
  Write-Host "zone up, wanted boss = $Boss (hp x $HpScale, back 20 s after a kill)"

  $hero = "play" + (Get-Random -Maximum 9999)
  $reg = Api-Call POST "/auth/register" @{ username = $hero; email = "$hero@example.com"; password = $password }
  Api-Call POST "/characters" @{ name = $hero; classId = $Class } $reg.accessToken | Out-Null
  & node (Join-Path $apiDir "scripts\dev-boost-character.mjs") --uri $mongoUri --db $dbName --name $hero --level $Level --large-potions 80 @($starts[$Boss]) | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "boost $hero failed" }

  $env:ZENONIA_TEST_PASSWORD = $password
  $client = Start-Process $Exe -PassThru -ArgumentList (@("-screen-fullscreen", 0, "-screen-width", 1280, "-screen-height", 853, "--mode", "client", "-logFile", "`"$OutDir\client.log`"", "--api-url", $api, "--account", $hero, "--name", $hero, "--net-bot", "persist", "--wanted-boss", $Boss, "--persist-role", $(if ($Bot) { "wanted-kill" } else { "idle" }), "--wanted-hp-scale", $HpScale) + $(if ($Bot) { @("--wanted-shots", "`"$OutDir\shots`"") } else { @() }))
  Remove-Item env:ZENONIA_TEST_PASSWORD
  Write-Host "client window open as $hero ($Class, level $Level). Close the game window to stop the servers."
  $client.WaitForExit()
}
finally {
  foreach ($p in $procs) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
  Get-Process mongod -ErrorAction SilentlyContinue | Where-Object { $_.Path -and (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine -match "$MongoPort" } | Stop-Process -Force -ErrorAction SilentlyContinue
  Write-Host "stopped"
}
