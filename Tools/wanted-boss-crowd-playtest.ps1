<#
.SYNOPSIS
  Crowd fight on the Wanted Boss, localhost: the stack of wanted-boss-playtest.ps1 (dev mongod, server-api, a headless zone pinned to one wanted boss), plus
  N light protocol bots (Tests/dotnet/ZenoniaSwarm, all in ONE process, no Unity) that log in as real level-60 characters with server-api tickets and
  attack the boss, plus ONE real client window (persist role "idle": the controls are yours) standing next to the boss.
  Close the game window to stop everything, or run the script again with -Stop (kills the pids saved in OutDir\pids.txt).
  Secrets and accounts are made up for this run and die with it (the database lives in OutDir).
.EXAMPLE
  .\Tools\wanted-boss-crowd-playtest.ps1 -Exe C:\temp\player-261117k\Zenonia.exe
  .\Tools\wanted-boss-crowd-playtest.ps1 -Exe C:\temp\player-261117k\Zenonia.exe -Bots 19 -HpScale 10
  .\Tools\wanted-boss-crowd-playtest.ps1 -Exe C:\temp\player-261117k\Zenonia.exe -Party
  .\Tools\wanted-boss-crowd-playtest.ps1 -Stop
  -Party: the window's hero joins a party of 4 with bots 4, 9 and 14 (Clerics: they buff Atk / Def and heal). The bots follow the hero;
  the window runs --net-bot party --party-role joiner, which accepts the first invite (the keys stay yours). See Tests/dotnet/ZenoniaSwarm/Program.Party.cs.
#>
param(
  [string]$Exe,
  [string]$OutDir = "C:\temp\wanted-40",
  [string]$Boss = "rimeclawTyrant",   # rimeclawTyrant or mireHydraQueen (boss points in $starts below)
  [string]$Class = "warrior",         # the window's hero; bots cycle warrior / wizard / mechanic / assassin / cleric
  [int]$Bots = 39,
  [double]$HpScale = 150,     # 1 = 34800 HP; 40 bots took ~110 s to kill x30, so x150 lasts ~9 min (the HP number on the bar caps at 65535: ushort in the hit event)
  [int]$Level = 60,
  [int]$ZonePort = 7799,
  [int]$ApiPort = 3199,
  [int]$MongoPort = 27199,
  [string]$Ui = "new",
  [switch]$Party,
  [switch]$Stop
)
$ErrorActionPreference = "Stop"
$pidFile = Join-Path $OutDir "pids.txt"
function Stop-Crowd {
  if (Test-Path $pidFile) { Get-Content $pidFile | ForEach-Object { Stop-Process -Id ([int]$_) -Force -ErrorAction SilentlyContinue } }
  Get-CimInstance Win32_Process -Filter "Name='mongod.exe'" | Where-Object { $_.CommandLine -match "$MongoPort" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
if ($Stop) { Stop-Crowd; Write-Host "stopped"; return }
if (-not $Exe) { throw "-Exe is required" }
$Exe = (Resolve-Path $Exe).Path
$repo = Split-Path $PSScriptRoot -Parent
$apiDir = Join-Path $repo "server-api"
$swarmDir = Join-Path $repo "Tests\dotnet\ZenoniaSwarm"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function New-Secret { -join ((1..48) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) }) }
$serviceKey = New-Secret; $jwtSecret = New-Secret; $password = "pw-" + (New-Secret)
$api = "http://127.0.0.1:$ApiPort"
$mongoUri = "mongodb://127.0.0.1:$MongoPort/?replicaSet=rs0"
$dbName = "wanted_crowd"
# Hero start (3 tiles south of the boss) and the boss point the bots aim at (WantedBoss.cs tile (64,29), 48 rows x 16 units)
$starts = @{
  rimeclawTyrant = @{ Map = "frostHollow"; X = 1032; Y = 248; Goal = "1032,296" }
  mireHydraQueen = @{ Map = "sunkenReliquary"; X = 488; Y = 120; Goal = "488,152" } # tile (30,14) of the 24-row Sunken Reliquary, hero 2 rows south
}
if (-not $starts.ContainsKey($Boss)) { throw "no start position for $Boss" }
$s = $starts[$Boss]
$classes = @("warrior", "wizard", "mechanic", "assassin", "cleric") # HeroClass order: the swarm picks melee / ranged reach from index % 5
$procs = @()
function Save-Pids { $procs | Where-Object { $_ } | ForEach-Object { $_.Id } | Set-Content $pidFile }

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
# A fresh account + character at level $Level standing at $x,$y; returns the access token and the character id
function New-Hero([string]$name, [string]$cls, [int]$x, [int]$y) {
  $reg = Api-Call POST "/auth/register" @{ username = $name; email = "$name@example.com"; password = $password }
  $ch = Api-Call POST "/characters" @{ name = $name; classId = $cls } $reg.accessToken
  & node (Join-Path $apiDir "scripts\dev-boost-character.mjs") --uri $mongoUri --db $dbName --name $name --level $Level --large-potions 80 --map $s.Map --x $x --y $y | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "boost $name failed" }
  return @{ Token = $reg.accessToken; Id = $ch.character.id; Name = $name }
}

$ok = $false
try {
  $swarmExe = Join-Path $swarmDir "bin\Release\net10.0\ZenoniaSwarm.exe"
  & dotnet build $swarmDir -c Release -v q -nologo | Out-Null
  if ($LASTEXITCODE -ne 0 -or -not (Test-Path $swarmExe)) { throw "ZenoniaSwarm did not build" }

  $procs += Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$OutDir\mongo-data`"") -RedirectStandardOutput "$OutDir\mongo.out" -RedirectStandardError "$OutDir\mongo.err"
  Save-Pids
  if (-not (Wait-Log "$OutDir\mongo.out" "READY" 60)) { throw "mongod did not start, see $OutDir\mongo.err" }
  Write-Host "mongod up"

  # Rate limits raised: one machine registers and logs in $Bots + 1 accounts
  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = $mongoUri; MONGO_DB = $dbName; JWT_SECRET = $jwtSecret; SERVICE_KEY = $serviceKey; ZONE_ID = "wanted-crowd"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; ACCESS_TTL_SEC = 3600; BCRYPT_COST = 4; REGISTER_IP_MAX = 1000; LOGIN_IP_MAX = 1000 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $procs += Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api.out" -RedirectStandardError "$OutDir\api.err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  Save-Pids
  $until = (Get-Date).AddSeconds(20); $up = $false
  while (-not $up -and (Get-Date) -lt $until) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; $up = $true } catch { Start-Sleep -Milliseconds 300 } }
  if (-not $up) { throw "server-api did not start, see $OutDir\api.err" }
  Write-Host "server-api up"

  $env:ZENONIA_SERVICE_KEY = $serviceKey
  $procs += Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "wanted-crowd", "--wanted-boss", $Boss, "--wanted-respawn-sec", 20, "--wanted-hp-scale", $HpScale, "-logFile", "`"$OutDir\zone.log`"")
  Remove-Item env:ZENONIA_SERVICE_KEY
  Save-Pids
  if (-not (Wait-Log "$OutDir\zone.log" "zone sessions reset" 40)) { throw "the zone never got ready, see $OutDir\zone.log" }
  Write-Host "zone up, wanted boss = $Boss (hp x $HpScale)"

  # Bots: spread over a 5 x 2 block south of the boss so they do not stand on one pixel
  $tag = Get-Random -Maximum 999
  $crowd = @()
  for ($i = 0; $i -lt $Bots; $i++) {
    $crowd += New-Hero ("c" + $tag + "b" + $i) $classes[$i % 5] ($s.X - 48 + ($i % 5) * 24) ($s.Y - 8 - [math]::Floor(($i / 5) % 2) * 16)
  }
  $hero = New-Hero ("play" + $tag) $Class $s.X $s.Y
  Write-Host "$Bots bot characters + $($hero.Name) ready (level $Level)"

  # Tickets live 60 s: take them all, then start the swarm at once
  $tickets = foreach ($b in $crowd) { (Api-Call POST "/characters/$($b.Id)/enter" @{} $b.Token).ticket }
  $tickets | Set-Content "$OutDir\tickets.txt"
  $procs += Start-Process $swarmExe -PassThru -WindowStyle Hidden -ArgumentList (@("--host", "127.0.0.1", "--port", $ZonePort, "--tokens", "`"$OutDir\tickets.txt`"", "--mix", "100:0:0", "--goal", $s.Goal, "--hair", "vary", "--stagger-ms", 60, "--seconds", 86400) + $(if ($Party) { @("--party-with", $hero.Name, "--party-bots", "4,9,14") } else { @() })) -RedirectStandardOutput "$OutDir\swarm.log" -RedirectStandardError "$OutDir\swarm.err"
  Save-Pids
  Write-Host "swarm started ($Bots bots)"

  $env:ZENONIA_TEST_PASSWORD = $password
  $client = Start-Process $Exe -PassThru -ArgumentList (@("-screen-fullscreen", 0, "-screen-width", 1280, "-screen-height", 853, "--mode", "client", "-logFile", "`"$OutDir\client.log`"", "--api-url", $api, "--account", $hero.Name, "--name", $hero.Name, "--wanted-boss", $Boss, "--wanted-hp-scale", $HpScale, "--ui", $Ui) + $(if ($Party) { @("--net-bot", "party", "--party-role", "joiner") } else { @("--net-bot", "persist", "--persist-role", "idle") }))
  Remove-Item env:ZENONIA_TEST_PASSWORD
  $procs += $client
  Save-Pids
  Write-Host "client window open as $($hero.Name). Close the game window to stop everything."
  $ok = $true
  $client.WaitForExit()
}
finally {
  if (-not $ok) { Write-Host "setup failed, stopping" }
  Stop-Crowd
  Write-Host "stopped"
}
