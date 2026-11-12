<#
.SYNOPSIS
  End-to-end check of the daily Wanted Boss on localhost: real mongod (server-api's dev-mongo, no Docker), real server-api, a headless zone pinned with --wanted-boss rimeclawTyrant (event boss, HP scaled down for the test), and scripted
  clients (DebugCapture.NetWanted.cs) that log in with the real flow. Every number it judges comes from the API, not from the bots. Secrets are made up for this run and die with it.
  A hero kills the wanted boss: gold / stones / potions went up by at least the table, wantedDay is today's key, a graceful zone restart keeps it, a second kill the same day pays nothing (the zone
  counts the claims in its log), a relog sees the claim. Then two real windows save PNGs of the chat lines, the toast and the board (claimed and not claimed) into OutDir\shots.
  Needs a player built from a copy (Tools/build-from-copy.ps1). Ports differ from net-persist-test.ps1.
.EXAMPLE
  .\Tools\wanted-boss-test.ps1 -Exe C:\temp\player-wanted\Zenonia.exe -OutDir C:\temp\wanted
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "wanted-test-out",
  [int]$ZonePort = 7797,
  [int]$ApiPort = 3197,
  [int]$MongoPort = 27197,
  [switch]$NoShots
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
$dbName = "wanted_e2e"
$shutdownFile = "$OutDir\stop-zone"
$suffix = Get-Random -Maximum 99999
$shotDir = "$OutDir\shots"
# Today's key, same formula as WantedBoss.DayKey (05:00 UTC+7 = 22:00 UTC)
$dayKey = [int][math]::Floor(([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 7200000) / 86400000)
# Rimeclaw Tyrant (Frost Hollow, Lv10): fixed reward 150 G, 3 stones, 3 + 3 Small potions. Its HP is 30 x the King (34800); the test scales it down so one hero can kill it in about a minute.
$hpScale = 0.03
$heroLevel = 60 # a lone hero must out-live a boss with 1.5 x the King's attack for about a minute
$reward = @{ gold = 150; stones = 3; hp = 3; mp = 3 }
# Frost Hollow, 48 rows x 16 units, saves of version 3. The Tyrant stands on tile (64,29): the hero starts three tiles south of it, x = (64 + .5) * 16, y = 768 - (32 + .5) * 16
$nearWanted = @("--map", "frostHollow", "--x", "1032", "--y", "248")
# The map boss (Frostfang King) is on tile (7,5): three tiles from it x = (10 + .5) * 16, y = 768 - (8 + .5) * 16
$nearKing = @("--map", "frostHollow", "--x", "168", "--y", "632")
# Oakhaven (24 rows): just below Guard Rook (tile 18,20)
$nearGuard = @("--map", "oakhavenVillage", "--x", "296", "--y", "70")
$script:passed = 0; $script:failed = 0; $script:apiProc = $null; $script:zoneProc = $null; $script:mongoProc = $null; $script:clients = @(); $script:runs = 0

function Ok([string]$what, [bool]$cond, [string]$detail = "") {
  if ($cond) { $script:passed++; Write-Host "PASS $what" } else { $script:failed++; Write-Host "FAIL $what  $detail" }
}
function Section([string]$s) { Write-Host ""; Write-Host "=== $s" }
function Stop-Tree($p) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; try { $p.WaitForExit(5000) | Out-Null } catch {} } }
function Wait-Log([string]$path, [string]$pattern, [int]$seconds) {
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) {
    if ((Test-Path $path) -and (Select-String -Path $path -Pattern $pattern -Quiet -ErrorAction SilentlyContinue)) { return $true }
    Start-Sleep -Milliseconds 300
  }
  return $false
}

function Start-Mongo {
  $script:mongoProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$OutDir\mongo-data`"") -RedirectStandardOutput "$OutDir\mongo.out" -RedirectStandardError "$OutDir\mongo.err"
  if (-not (Wait-Log "$OutDir\mongo.out" "READY" 60)) { throw "mongod did not start, see $OutDir\mongo.err" }
}
function Start-Api {
  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = $mongoUri; MONGO_DB = $dbName; JWT_SECRET = $jwtSecret; SERVICE_KEY = $serviceKey; ZONE_ID = "wanted-zone"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; ACCESS_TTL_SEC = 600; REGISTER_IP_MAX = 500; LOGIN_IP_MAX = 5000; BCRYPT_COST = 6 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $script:apiProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api.out" -RedirectStandardError "$OutDir\api.err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  $until = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $until) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; return } catch { Start-Sleep -Milliseconds 300 } }
  throw "server-api did not start, see $OutDir\api.err"
}
function Start-Zone {
  $script:runs++
  Remove-Item $shutdownFile -ErrorAction SilentlyContinue
  $script:zoneLog = "$OutDir\zone-$($script:runs).log"
  $env:ZENONIA_SERVICE_KEY = $serviceKey
  $script:zoneProc = Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "wanted-zone", "--save-interval", 5, "--save-spool", "`"$OutDir\spool`"", "--shutdown-file", "`"$shutdownFile`"", "--wanted-boss", "rimeclawTyrant", "--wanted-respawn-sec", 25, "--wanted-hp-scale", $hpScale, "-logFile", "`"$($script:zoneLog)`"")
  Remove-Item env:ZENONIA_SERVICE_KEY
  if (-not (Wait-Log $script:zoneLog "zone sessions reset" 40)) { throw "the zone never got ready, see $($script:zoneLog)" }
}
# A graceful stop: the zone saves everybody and exits.
function Stop-ZoneGracefully {
  New-Item -ItemType File -Path $shutdownFile | Out-Null
  return $script:zoneProc.WaitForExit(40000)
}
# Paid claims a zone logged ("wanted: <hero> claimed <boss> day N: ...", NetPlayer.OnWantedClaimed).
function Claims-Of([string]$zoneLog) { return @(Select-String -Path $zoneLog -Pattern "] wanted: .* claimed " -ErrorAction SilentlyContinue).Count }

function Api-Call([string]$method, [string]$path, $body = $null, [string]$token = $null) {
  $h = @{}
  if ($token) { $h.Authorization = "Bearer $token" }
  $json = if ($null -ne $body) { $body | ConvertTo-Json -Depth 6 } elseif ($method -eq "POST") { "{}" } else { $null }
  try {
    $r = Invoke-WebRequest "$api$path" -Method $method -Headers $h -Body $json -ContentType "application/json" -UseBasicParsing -TimeoutSec 8
    return @{ Status = [int]$r.StatusCode; Body = ($r.Content | ConvertFrom-Json) }
  } catch { return @{ Status = $(if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }); Body = $null } }
}
function Register-Account([string]$name, [string]$class) {
  $r = Api-Call POST "/auth/register" @{ username = $name; email = "$name@example.com"; password = $password }
  if ($r.Status -ne 201) { throw "register $name -> $($r.Status)" }
  $c = Api-Call POST "/characters" @{ name = $name; classId = $class } $r.Body.accessToken
  if ($c.Status -ne 201) { throw "create $name -> $($c.Status)" }
}
function Read-Character([string]$name) {
  $t = (Api-Call POST "/auth/login" @{ login = $name; password = $password }).Body.accessToken
  $id = @((Api-Call GET "/characters" $null $t).Body.characters)[0].id
  return (Api-Call GET "/characters/$id" $null $t).Body.character
}
function Wait-Offline([string]$name, [int]$seconds) {
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) { if (-not (Read-Character $name).online) { return $true }; Start-Sleep -Milliseconds 700 }
  return $false
}
function Boost([string]$name, [string[]]$more = @()) {
  & node (Join-Path $apiDir "scripts\dev-boost-character.mjs") --uri $mongoUri --db $dbName --name $name --level $heroLevel --large-potions 80 @more | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "boost $name failed" }
}

# window = $true opens a real window (screenshots need one); otherwise headless.
function Start-Bot([string]$name, [string]$account, [string]$role, [bool]$window, [string[]]$more = @()) {
  $log = "$OutDir\$name.log"
  $env:ZENONIA_TEST_PASSWORD = $password
  $base = @("--mode", "client", "-logFile", "`"$log`"", "--api-url", $api, "--account", $account, "--name", $account, "--net-bot", "persist", "--persist-role", $role, "--wanted-hp-scale", $hpScale)
  $pre = if ($window) { @("-screen-fullscreen", 0, "-screen-width", 960, "-screen-height", 640) } else { @("-batchmode", "-nographics") }
  $p = Start-Process $Exe -PassThru -ArgumentList ($pre + $base + $more)
  Remove-Item env:ZENONIA_TEST_PASSWORD
  $script:clients += $p
  return @{ Proc = $p; Log = $log }
}
function Wait-Exit($c, [int]$seconds) { if (-not $c.Proc.WaitForExit($seconds * 1000)) { Stop-Tree $c.Proc; return $false }; return $true }
function Show-Lines($c) {
  Select-String -Path $c.Log -Pattern "persist-e2e: " -ErrorAction SilentlyContinue | ForEach-Object {
    $m = $_.Line -replace "^.*persist-e2e: ", ""
    if ($m -match "^PASS") { $script:passed++ } elseif ($m -match "^FAIL") { $script:failed++ }
    Write-Host "  bot: $m"
  }
}
function Copy-Failures($c) { Select-String -Path $c.Log -Pattern "NullReference|Exception" -ErrorAction SilentlyContinue | ForEach-Object { $script:failed++; Write-Host ("FAIL in " + (Split-Path $c.Log -Leaf) + ": " + $_.Line) } }
function Line-Of($c, [string]$pattern) { (Select-String -Path $c.Log -Pattern $pattern -ErrorAction SilentlyContinue | Select-Object -Last 1).Line }

try {
  Section "stack"
  Start-Mongo; Write-Host "mongod up (replica set, not Docker)"
  Start-Api; Write-Host "server-api up"
  Start-Zone; Write-Host "zone up, wanted boss pinned to rimeclawTyrant"
  Ok "the zone warns that the wanted boss is pinned" (Wait-Log $script:zoneLog "WANTED BOSS PINNED to rimeclawTyrant" 2)
  Write-Host "today's day key = $dayKey"

  Section "first kill pays once (real window, pictures of the chat and the toast)"
  $hero = "wan$suffix"
  Register-Account $hero "warrior"
  Boost $hero $nearWanted
  $before = (Read-Character $hero).state
  Ok "a fresh character has wantedDay 0" ($before.wantedDay -eq 0) "wantedDay=$($before.wantedDay)"
  $shotArgs = if ($NoShots) { @() } else { @("--wanted-shots", "`"$shotDir`"") }
  $shotArgs += "--wanted-respawn-check" # after the kill it waits for the boss to come back (the zone runs --wanted-respawn-sec 25)
  $c = Start-Bot "hero-1-kill" $hero "wanted-kill" (-not $NoShots) $shotArgs
  Ok "the kill round finished" (Wait-Exit $c 330)
  Show-Lines $c; Copy-Failures $c
  Ok "left" (Wait-Offline $hero 40)
  $after = (Read-Character $hero).state
  Ok "the API has wantedDay = today ($dayKey)" ($after.wantedDay -eq $dayKey) "wantedDay=$($after.wantedDay)"
  Ok "gold went up by at least the reward" (($after.gold - $before.gold) -ge $reward.gold) "gold $($before.gold) -> $($after.gold)"
  Ok "stones went up by at least the reward" (($after.upgradeStone - $before.upgradeStone) -ge $reward.stones) "stones $($before.upgradeStone) -> $($after.upgradeStone)"
  Ok "Small HP potions went up by at least the reward" (($after.hpPotion - $before.hpPotion) -ge $reward.hp) "hp $($before.hpPotion) -> $($after.hpPotion)"
  Ok "Small MP potions went up by at least the reward" (($after.mpPotion - $before.mpPotion) -ge $reward.mp) "mp $($before.mpPotion) -> $($after.mpPotion)"
  Write-Host "  API deltas: gold +$($after.gold - $before.gold), stones +$($after.upgradeStone - $before.upgradeStone), hp +$($after.hpPotion - $before.hpPotion), mp +$($after.mpPotion - $before.mpPotion) (reward: 150 / 3 / 3 / 3, the boss's own drops are on top)"

  Section "the map boss pays no wanted reward"
  $kinger = "wmk$suffix"
  Register-Account $kinger "warrior"
  Boost $kinger $nearKing
  $c = Start-Bot "king-1-map-boss" $kinger "wanted-kill" $false @("--wanted-target", "map", "--wanted-expect", "none")
  Ok "the map boss round finished" (Wait-Exit $c 330)
  Show-Lines $c; Copy-Failures $c
  Ok "left" (Wait-Offline $kinger 40)
  Ok "the API has wantedDay 0 for the hero that killed the Frostfang King" ((Read-Character $kinger).state.wantedDay -eq 0) "wantedDay=$((Read-Character $kinger).state.wantedDay)"

  Section "a graceful zone restart keeps the claim, a second kill the same day pays nothing"
  Ok "the zone stops by itself" (Stop-ZoneGracefully)
  Ok "zone 1 paid exactly one claim (the Tyrant kill; the map boss kill paid none)" ((Claims-Of "$OutDir\zone-1.log") -eq 1) "claims=$(Claims-Of "$OutDir\zone-1.log")"
  Start-Zone # the boss spawns again at the start of the zone
  $kept = (Read-Character $hero).state
  Ok "wantedDay and gold survived the restart" ($kept.wantedDay -eq $dayKey -and $kept.gold -eq $after.gold) "wantedDay=$($kept.wantedDay) gold=$($kept.gold)"
  Boost $hero $nearWanted
  $before2 = (Read-Character $hero).state
  $c = Start-Bot "hero-2-second" $hero "wanted-kill" $false @("--wanted-expect", "none")
  Ok "the second kill round finished" (Wait-Exit $c 330)
  Show-Lines $c; Copy-Failures $c
  Ok "the relogged client knew it had claimed (day in its first state)" ((Line-Of $c "wanted-before") -match "day=$dayKey ") (Line-Of $c "wanted-before")
  Ok "left" (Wait-Offline $hero 40)
  $after2 = (Read-Character $hero).state
  Ok "the API still has wantedDay = today" ($after2.wantedDay -eq $dayKey) "wantedDay=$($after2.wantedDay)"
  Write-Host "  second kill, API deltas: gold +$($after2.gold - $before2.gold), stones +$($after2.upgradeStone - $before2.upgradeStone), hp +$($after2.hpPotion - $before2.hpPotion), mp +$($after2.mpPotion - $before2.mpPotion) (normal drops only)"
  Ok "zone 2 stops by itself" (Stop-ZoneGracefully)
  Ok "zone 2 paid no claim" ((Claims-Of "$OutDir\zone-2.log") -eq 0) "claims=$(Claims-Of "$OutDir\zone-2.log")"

  if (-not $NoShots) {
    Section "the board in real windows"
    Start-Zone
    $fresh = "wnb$suffix"
    Register-Account $fresh "wizard"
    $w = Start-Bot "board-1-not-claimed" $fresh "wanted-board" $true @("--wanted-shots", "`"$shotDir\not-claimed`"")
    Ok "the not-claimed board round finished" (Wait-Exit $w 150)
    Show-Lines $w; Copy-Failures $w
    Ok "left" (Wait-Offline $fresh 40)
    Boost $hero $nearGuard
    $w = Start-Bot "board-2-claimed" $hero "wanted-board" $true @("--wanted-shots", "`"$shotDir\claimed`"")
    Ok "the claimed board round finished" (Wait-Exit $w 150)
    Show-Lines $w; Copy-Failures $w
    Ok "left" (Wait-Offline $hero 40)
    Write-Host "pictures in $shotDir"
  }
}
catch { $script:failed++; Write-Host "FAIL the script stopped: $_" }
finally {
  foreach ($p in $script:clients) { Stop-Tree $p }
  Stop-Tree $script:zoneProc
  Stop-Tree $script:apiProc
  Stop-Tree $script:mongoProc
  Get-CimInstance Win32_Process -Filter "Name LIKE 'mongod%'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*--port $MongoPort*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
Write-Host ""
Write-Host "--- $($script:passed) passed, $($script:failed) failed"
if ($script:failed -gt 0) { exit 1 }
