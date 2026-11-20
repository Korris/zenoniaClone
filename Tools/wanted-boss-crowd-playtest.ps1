<#
.SYNOPSIS
  Crowd fight on the Wanted Boss, localhost: the stack of wanted-boss-playtest.ps1 (dev mongod, server-api, a headless zone pinned to one wanted boss), plus
  N light protocol bots (Tests/dotnet/ZenoniaSwarm, all in ONE process, no Unity) that log in as real level-60 characters with server-api tickets and
  attack the boss, plus ONE real client window (persist role "idle": the controls are yours) standing next to the boss.
  The bots start on walkable tiles in a ring 3-6 tiles around the boss (ZenoniaSwarm --free-ring reads the real map rows), the window's hero 2-3 tiles south of it.
  -GodBots (on by default; -GodBots $false turns it off): the zone runs with --wanted-god-bots <bot name prefix>, so the bots take no damage and never respawn far away.
  The zone only honours that on a test build with a pinned Wanted Boss and a loopback server-api. The window's hero stays mortal unless -GodHero
  (a level-60 hero with the starter weapon dies to the boss in about a minute, then respawns at the map entrance and the camera leaves the fight).
  -Formation (on by default): the bots walk from their spawn tile to a slot on a ring around the boss by class reach (Program.Formation.cs) and attack from there.
  Another crowd can run beside a running one: give it its own -OutDir, -ZonePort, -ApiPort and -MongoPort.
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
  [string]$Ui = "",           # "" = the game default (RO Soft Fantasy); new = the old Adventure Paper screens (UiScreens); old = classic
  [switch]$Party,
  [bool]$GodBots = $true,     # bots take no damage (test zone hook --wanted-god-bots)
  [switch]$GodHero,           # the window's hero takes no damage either (off by default)
  [bool]$Formation = $true,   # bots hold ring slots around the boss by class reach (swarm --formation): melee close, Wizard / Cleric, then Mechanic outside
  [string]$Role = "",         # a persist role for the window instead of "idle", e.g. wanted-showcase (shots into OutDir\shots)
  [int]$ScreenW = 1280,       # the client window size (other aspects: 1024x768, 1280x720, 1560x720 phone)
  [int]$ScreenH = 853,
  [string[]]$ExtraArgs = @(), # more client window arguments, e.g. -ExtraArgs "--view25d","--fps-log"
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
# Hero start (south of the boss) and the boss point the bots aim at; Tile = the boss tile of WantedBoss.cs, HeroTile = the hero's (16 units a tile, world y grows upward)
$starts = @{
  rimeclawTyrant = @{ Map = "frostHollow"; X = 1032; Y = 248; Goal = "1032,296"; Tile = "64,29"; HeroTile = "64,32" } # 48 rows, hero 3 rows south
  mireHydraQueen = @{ Map = "sunkenReliquary"; X = 728; Y = 152; Goal = "728,184"; Tile = "45,12"; HeroTile = "45,14" } # tile (45,12) of the 58x24 Sunken Reliquary (middle of the wide arena), hero 2 rows south
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

  $tag = Get-Random -Maximum 999
  $botPrefix = "c" + $tag + "b" # bot names c<tag>b<i>; the window's hero is play<tag>, outside the prefix
  $env:ZENONIA_SERVICE_KEY = $serviceKey
  $procs += Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList (@("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "wanted-crowd", "--wanted-boss", $Boss, "--wanted-respawn-sec", 20, "--wanted-hp-scale", $HpScale, "-logFile", "`"$OutDir\zone.log`"") + $(if ($GodBots -or $GodHero) { @("--wanted-god-bots", ((@($(if ($GodBots) { $botPrefix }), $(if ($GodHero) { "play" + $tag })) | Where-Object { $_ }) -join ",")) } else { @() }))
  Remove-Item env:ZENONIA_SERVICE_KEY
  Save-Pids
  if (-not (Wait-Log "$OutDir\zone.log" "zone sessions reset" 40)) { throw "the zone never got ready, see $OutDir\zone.log" }
  if (($GodBots -or $GodHero) -and -not (Wait-Log "$OutDir\zone.log" "GOD BOTS on" 5)) { throw "the zone refused --wanted-god-bots, see $OutDir\zone.log" }
  Write-Host "zone up, wanted boss = $Boss (hp x $HpScale), god bots = $GodBots, god hero = $GodHero"

  # Bots: one walkable tile each (map rows + prop footprints, reachable from the boss), 3-6 tiles around the boss, spread by angle, the hero's tile left out
  $ring = @(& $swarmExe --free-ring $s.Map --center $s.Tile --radius "3,6" --count $Bots --skip $s.HeroTile)
  if ($LASTEXITCODE -ne 0 -or $ring.Count -eq 0) { throw "no free tiles around the boss" }
  $crowd = @()
  for ($i = 0; $i -lt $Bots; $i++) {
    $p = $ring[$i % $ring.Count].Split(" ") # more bots than tiles: they share, a few units apart
    $crowd += New-Hero ($botPrefix + $i) $classes[$i % 5] ([int]$p[0] + [math]::Floor($i / $ring.Count) * 4) ([int]$p[1])
  }
  $hero = New-Hero ("play" + $tag) $Class $s.X $s.Y
  Write-Host "$Bots bot characters + $($hero.Name) ready (level $Level)"

  # Tickets live 60 s: take them all, then start the swarm at once
  $tickets = foreach ($b in $crowd) { (Api-Call POST "/characters/$($b.Id)/enter" @{} $b.Token).ticket }
  $tickets | Set-Content "$OutDir\tickets.txt"
  $procs += Start-Process $swarmExe -PassThru -WindowStyle Hidden -ArgumentList (@("--host", "127.0.0.1", "--port", $ZonePort, "--tokens", "`"$OutDir\tickets.txt`"", "--mix", "100:0:0", "--goal", $s.Goal, "--map", $s.Map, "--hair", "vary", "--stagger-ms", 60, "--seconds", 86400) + $(if ($Formation) { @("--formation") } else { @() }) + $(if ($Party) { @("--party-with", $hero.Name, "--party-bots", "4,9,14") } else { @() })) -RedirectStandardOutput "$OutDir\swarm.log" -RedirectStandardError "$OutDir\swarm.err"
  Save-Pids
  Write-Host "swarm started ($Bots bots)"

  $env:ZENONIA_TEST_PASSWORD = $password
  $client = Start-Process $Exe -PassThru -ArgumentList (@("-screen-fullscreen", 0, "-screen-width", $ScreenW, "-screen-height", $ScreenH, "--mode", "client", "-logFile", "`"$OutDir\client.log`"", "--api-url", $api, "--account", $hero.Name, "--name", $hero.Name, "--wanted-boss", $Boss, "--wanted-hp-scale", $HpScale) + $(if ($Ui) { @("--ui", $Ui) } else { @() }) + $(if ($Party) { @("--net-bot", "party", "--party-role", "joiner") } elseif ($Role) { @("--net-bot", "persist", "--persist-role", $Role, "--wanted-shots", "`"$OutDir\shots`"") } else { @("--net-bot", "persist", "--persist-role", "idle") }) + $ExtraArgs)
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
