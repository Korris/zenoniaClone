<#
.SYNOPSIS
  End-to-end check of Card Master Orin's "The First Card Duel" (Taurus V4: must WIN, a loss / draw / give-up replays for free, the winner picks ONE of Orin's 8 cards once) on real accounts, localhost: a real mongod
  (server-api's dev-mongo, no Docker), the real server-api, a headless zone server and scripted clients (DebugCapture.NetOrin.cs, --net-bot orin) that log in with the real flow. What it judges comes from the API (the
  stored character), not from the clients. Every character is seeded (dev-seed.mjs) as one that finished Rowan's trial and Orin's first-deck quest: the 5 starter cards in the collection, the duel quest never taken.
  Characters:
    full     accept at Orin (accept + duel request back to back), the scripted duel (a move off the script is refused), replays until a win, a bad pick index is refused, the pick pays exactly 1 chosen card and
             closes the quest; a second client run (Done) changes nothing, a tutorial duel is refused, a practice duel changes nothing
    drop     the client is KILLED after the first lesson move: the quest stays Active in the API, nothing paid; the character logs in again, plays until it wins and is paid once
    forfeit  gives up at once: the quest stays Active (a give-up is not a win), nothing paid; the bot replays, wins and is paid once
  Prints PASS / FAIL lines and a count. Needs a player built from a copy (Tools/build-from-copy.ps1). Secrets are made up for this run and die with it.
.EXAMPLE
  .\Tools\net-orin-test.ps1 -Exe C:\temp\player-taurus\Zenonia.exe -OutDir C:\temp\orin-test
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "orin-test-out",
  [int]$ZonePort = 7797,
  [int]$ApiPort = 3197,
  [int]$MongoPort = 27197,
  [string]$ShotDir = "" # with it: one more character plays in a real window and saves PNGs of the online screens there
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
$repo = Split-Path $PSScriptRoot -Parent
$apiDir = Join-Path $repo "server-api"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue

function New-Secret { -join ((1..48) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) }) }
$serviceKey = New-Secret
$jwtSecret = New-Secret
$password = "pw-" + (New-Secret)
$api = "http://127.0.0.1:$ApiPort"
$mongoUri = "mongodb://127.0.0.1:$MongoPort/?replicaSet=rs0"
$dbName = "orin_e2e"
$suffix = Get-Random -Maximum 99999
$script:passed = 0; $script:failed = 0; $script:apiProc = $null; $script:zoneProc = $null; $script:mongoProc = $null; $script:clients = @()
$starterCards = @("tauCloverSlime", "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauLeafMantis")
$orinCards = @("tauCloverSlime", "tauBrambleBoar", "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauLeafMantis", "tauRootling", "tauAmberSporeling") # the 8 the winner picks from (the bot picks tauBrambleBoar)

function Ok([string]$what, [bool]$cond, [string]$detail = "") { if ($cond) { $script:passed++; Write-Host "PASS $what" } else { $script:failed++; Write-Host "FAIL $what  $detail" } }
function Section([string]$s) { Write-Host ""; Write-Host "=== $s" }
function Stop-Tree($p) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; try { $p.WaitForExit(5000) | Out-Null } catch {} } }
function Wait-Log([string]$path, [string]$pattern, [int]$seconds) {
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) { if ((Test-Path $path) -and (Select-String -Path $path -Pattern $pattern -Quiet -ErrorAction SilentlyContinue)) { return $true }; Start-Sleep -Milliseconds 300 }
  return $false
}
function Start-Mongo {
  $script:mongoProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$OutDir\mongo-data`"") -RedirectStandardOutput "$OutDir\mongo.out" -RedirectStandardError "$OutDir\mongo.err"
  if (-not (Wait-Log "$OutDir\mongo.out" "READY" 60)) { throw "mongod did not start, see $OutDir\mongo.err" }
}
function Start-Api {
  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = $mongoUri; MONGO_DB = $dbName; JWT_SECRET = $jwtSecret; SERVICE_KEY = $serviceKey; ZONE_ID = "e2e-zone"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; ACCESS_TTL_SEC = 600; REGISTER_IP_MAX = 500; LOGIN_IP_MAX = 5000; BCRYPT_COST = 6 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $script:apiProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api.out" -RedirectStandardError "$OutDir\api.err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  $until = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $until) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; return } catch { Start-Sleep -Milliseconds 300 } }
  throw "server-api did not start, see $OutDir\api.err"
}
function Start-Zone {
  $script:zoneLog = "$OutDir\zone.log"
  $env:ZENONIA_SERVICE_KEY = $serviceKey
  $script:zoneProc = Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "e2e-zone", "--save-interval", 3, "--save-spool", "`"$OutDir\spool`"", "-logFile", "`"$($script:zoneLog)`"")
  Remove-Item env:ZENONIA_SERVICE_KEY
  if (-not (Wait-Log $script:zoneLog "zone sessions reset" 40)) { throw "the zone never got ready, see $($script:zoneLog)" }
}
function Api-Call([string]$method, [string]$path, $body = $null, [string]$token = $null) {
  $h = @{}; if ($token) { $h.Authorization = "Bearer $token" }
  $json = if ($null -ne $body) { $body | ConvertTo-Json -Depth 6 } elseif ($method -eq "POST") { "{}" } else { $null }
  try { $r = Invoke-WebRequest "$api$path" -Method $method -Headers $h -Body $json -ContentType "application/json" -UseBasicParsing -TimeoutSec 8; return @{ Status = [int]$r.StatusCode; Body = ($r.Content | ConvertFrom-Json) } }
  catch { return @{ Status = $(if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }); Body = $null } }
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
function Seed([string[]]$a) { & node (Join-Path $apiDir "scripts\dev-seed.mjs") --uri $mongoUri --db $dbName @a | Out-Null }
# What the API stores: gold, the count of each of Orin's 8 cards and the duel quest state (0 = never taken, 1 Active, 2 ReadyToTurnIn, 3 Done).
function Facts([string]$name) {
  $s = (Read-Character $name).state
  $q = @($s.quests | Where-Object { $_.id -eq "first-card-duel" })
  $n = @{}; foreach ($id in $orinCards) { $n[$id] = [int](@($s.cards | Where-Object { $_.id -eq $id } | ForEach-Object { $_.n }) | Measure-Object -Sum).Sum }
  return @{ Gold = [int]$s.gold; Quest = $(if ($q.Count -gt 0) { [int]$q[0].state } else { 0 }); Cards = $n; Help = [int]$s.cardHelp }
}
function Total([hashtable]$f) { return [int](($f.Cards.Values | Measure-Object -Sum).Sum) }
function Show([hashtable]$f) { return "gold=$($f.Gold) quest=$($f.Quest) boar=$($f.Cards.tauBrambleBoar) rootling=$($f.Cards.tauRootling) sporeling=$($f.Cards.tauAmberSporeling) total=$(Total $f)" }
function Start-Orin([string]$tag, [string]$account, [string[]]$more = @()) {
  $log = "$OutDir\$tag.log"
  $env:ZENONIA_TEST_PASSWORD = $password
  $p = Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList (@("-batchmode", "-nographics", "--mode", "client", "-logFile", "`"$log`"", "--api-url", $api, "--account", $account, "--name", $account, "--net-bot", "orin") + $more)
  Remove-Item env:ZENONIA_TEST_PASSWORD
  $script:clients += $p
  return @{ Proc = $p; Log = $log }
}
function Finish([hashtable]$c, [int]$seconds) { if (-not $c.Proc.WaitForExit($seconds * 1000)) { Stop-Tree $c.Proc; return $false }; return $true }
function Lines([hashtable]$c) {
  Select-String -Path $c.Log -Pattern "orin-e2e:" -ErrorAction SilentlyContinue | ForEach-Object { $m = $_.Line -replace "^.*orin-e2e: ", ""; Write-Host "  [bot] $m"; if ($m -match "^FAIL") { $script:failed++; Write-Host "FAIL (bot) $m" } elseif ($m -match "^OK") { $script:passed++ } }
  Select-String -Path $c.Log -Pattern "NullReference|Exception" -ErrorAction SilentlyContinue | ForEach-Object { $script:failed++; Write-Host "FAIL exception in $(Split-Path $c.Log -Leaf): $($_.Line)" }
}
function Setup-Hero([string]$name, [string]$class) {
  Register-Account $name $class
  Seed @("--name", $name, "--card", (($starterCards | ForEach-Object { "$($_):1" }) -join ","), "--quest", "tau-meadow-trial:3:5,tau-orin-first-deck:3:5") # Rowan's trial and Orin's first deck done, the 5 starter cards held
}
function Expect-Paid([string]$who, [hashtable]$before, [hashtable]$after) {
  Ok "$who got exactly 1 tauBrambleBoar (the picked card)" ($after.Cards.tauBrambleBoar -eq $before.Cards.tauBrambleBoar + 1) "$(Show $before) -> $(Show $after)"
  Ok "$who got no other card (total +1)" ((Total $after) -eq (Total $before) + 1) "$(Show $before) -> $(Show $after)"
  Ok "$who rootling / sporeling (the other borrowed cards) were not given" ($after.Cards.tauRootling -eq 0 -and $after.Cards.tauAmberSporeling -eq 0) (Show $after)
  Ok "$who gold unchanged (this reward is a card only)" ($after.Gold -eq $before.Gold) "$(Show $before) -> $(Show $after)"
  Ok "$who quest is Done in the API" ($after.Quest -eq 3) (Show $after)
}

try {
  Section "stack"
  Start-Mongo; Write-Host "mongod up"
  Start-Api; Write-Host "server-api up"
  Start-Zone; Write-Host "zone up"

  Section "full: accept + immediate duel request, scripted duel, hand in once, nothing more on a second run, practice free"
  $full = "orf$suffix"
  Setup-Hero $full "warrior"
  $before = Facts $full
  Ok "start: the quest was never taken, the 5 starter cards held, no borrowed card" ($before.Quest -eq 0 -and $before.Cards.tauBrambleBoar -eq 0 -and (Total $before) -eq 5) (Show $before)
  $c = Start-Orin "full-1" $full
  Ok "the bot finished" (Finish $c 150)
  Lines $c
  Ok "the bot is gone" (Wait-Offline $full 30)
  $after = Facts $full
  Expect-Paid "full" $before $after
  Ok "the first-time card hint bits are intact" ($after.Help -ge 0)
  $c = Start-Orin "full-2" $full
  Ok "second run finished" (Finish $c 120)
  Lines $c
  Ok "the second run is gone" (Wait-Offline $full 30)
  $again = Facts $full
  Ok "a second run (quest Done, practice duel) pays nothing" ($again.Gold -eq $after.Gold -and (Total $again) -eq (Total $after) -and $again.Cards.tauBrambleBoar -eq $after.Cards.tauBrambleBoar -and $again.Quest -eq 3) "$(Show $after) -> $(Show $again)"

  Section "drop: the client dies mid-duel, the quest stays Active, the character plays it again and is paid once"
  $drop = "ord$suffix"
  Setup-Hero $drop "wizard"
  $before = Facts $drop
  $c = Start-Orin "drop-1" $drop @("--orin-drop")
  Ok "the bot reached the drop point" (Wait-Log $c.Log "orin-e2e: DROP" 120)
  Stop-Tree $c.Proc
  Ok "the zone let the character go" (Wait-Offline $drop 40)
  Start-Sleep -Seconds 5
  $mid = Facts $drop
  Ok "after the kill: the quest is Active, nothing paid, no card gained or lost" ($mid.Quest -eq 1 -and $mid.Gold -eq $before.Gold -and (Total $mid) -eq (Total $before)) (Show $mid)
  Ok "no zone exception for the dropped duel" (-not (Select-String -Path $script:zoneLog -Pattern "NullReference|Exception" -Quiet))
  $c = Start-Orin "drop-2" $drop
  Ok "the second login finished" (Finish $c 150)
  Lines $c
  Ok "the second login is gone" (Wait-Offline $drop 30)
  Expect-Paid "drop" $before (Facts $drop)

  Section "forfeit: giving up is not a win (the quest stays Active), the replay wins and is paid once"
  $ff = "orq$suffix"
  Setup-Hero $ff "warrior"
  $before = Facts $ff
  $c = Start-Orin "forfeit-1" $ff @("--orin-forfeit")
  Ok "the bot finished" (Finish $c 150)
  Lines $c
  Ok "the bot is gone" (Wait-Offline $ff 30)
  $mid = Facts $ff
  Ok "the give-up and the replays ended with the quest Done, one card" ($mid.Quest -eq 3 -and (Total $mid) -eq (Total $before) + 1) "$(Show $before) -> $(Show $mid)"
  Ok "the bot saw the give-up leave the quest Active" (Select-String -Path $c.Log -Pattern "orin-e2e: OK attempt 1 was not won" -Quiet)
  Expect-Paid "forfeit" $before $mid

  Section "before Rowan's trial and Orin's deck: the duel quest cannot be taken"
  $nc = "orn$suffix"
  Register-Account $nc "warrior"
  $c = Start-Orin "nocard-1" $nc @("--orin-peek")
  Ok "peek finished" (Finish $c 90)
  Lines $c
  Ok "no first deck, no quest in the API" ((Facts $nc).Quest -eq 0)
  Ok "no exception in the zone log" (-not (Select-String -Path $script:zoneLog -Pattern "NullReference|Exception" -Quiet))

  if ($ShotDir -ne "") {
    Section "screens: a real window, online (PNGs in $ShotDir)"
    New-Item -ItemType Directory -Force -Path $ShotDir | Out-Null
    $sh = "ors$suffix"
    Setup-Hero $sh "warrior"
    $log = "$OutDir\shots-1.log"
    $env:ZENONIA_TEST_PASSWORD = $password
    $p = Start-Process $Exe -PassThru -ArgumentList @("--mode", "client", "-screen-fullscreen", 0, "-screen-width", 1280, "-screen-height", 720, "-logFile", "`"$log`"", "--api-url", $api, "--account", $sh, "--name", $sh, "--net-bot", "orin", "--orin-shots", "--shot-path", "`"$ShotDir`"")
    Remove-Item env:ZENONIA_TEST_PASSWORD
    $script:clients += $p
    Ok "the window run finished" (Finish @{ Proc = $p; Log = $log } 200)
    Lines @{ Proc = $p; Log = $log }
    Ok "the window run ended with the quest Done and one card" ((Facts $sh).Quest -eq 3)
  }
}
finally {
  foreach ($p in $script:clients) { Stop-Tree $p }
  Stop-Tree $script:zoneProc; Stop-Tree $script:apiProc; Stop-Tree $script:mongoProc
}
Write-Host ""
Write-Host "RESULT: $($script:passed) passed, $($script:failed) failed"
if ($script:failed -gt 0) { exit 1 }
