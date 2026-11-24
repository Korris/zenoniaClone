<#
.SYNOPSIS
  End-to-end check of the card tutorial quest (Card Master Orin, "The First Card Duel") on real accounts, localhost: a real mongod (server-api's dev-mongo, no Docker), the real server-api, a headless zone server
  and scripted clients (DebugCapture.NetOrin.cs, --net-bot orin) that log in with the real flow. What it judges comes from the API (the stored character), not from the clients.
  Characters (each is registered with ONE card, so the quest is offered):
    full     accept at Orin, the scripted duel (a move off the script is refused), hand in: +300 G and hornRabbit / thornBoar / forestWolf exactly once; a second client run (Done) changes nothing,
             a tutorial duel is refused, a practice duel changes nothing
    drop     the client is KILLED after the first lesson move: the quest stays Active in the API, nothing paid; the character logs in again, plays the whole duel and is paid once
    forfeit  gives up at once: the quest still moves (decision: a duel given up counts), hand in pays once
  Prints PASS / FAIL lines and a count. Needs a player built from a copy (Tools/build-from-copy.ps1). Secrets are made up for this run and die with it.
.EXAMPLE
  .\Tools\net-orin-test.ps1 -Exe C:\temp\player-card-tutorial\Zenonia.exe -OutDir C:\temp\orin-test
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
$rewardCards = @("hornRabbit", "thornBoar", "forestWolf")

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
# What the API stores: gold, the reward cards and the quest state (0 = never taken, 1 Active, 2 ReadyToTurnIn, 3 Done).
function Facts([string]$name) {
  $s = (Read-Character $name).state
  $q = @($s.quests | Where-Object { $_.id -eq "first-card-duel" })
  $n = @{}; foreach ($id in $rewardCards) { $n[$id] = [int](@($s.cards | Where-Object { $_.id -eq $id } | ForEach-Object { $_.n }) | Measure-Object -Sum).Sum }
  return @{ Gold = [int]$s.gold; Quest = $(if ($q.Count -gt 0) { [int]$q[0].state } else { 0 }); Cards = $n; Help = [int]$s.cardHelp }
}
function Show([hashtable]$f) { return "gold=$($f.Gold) quest=$($f.Quest) hornRabbit=$($f.Cards.hornRabbit) thornBoar=$($f.Cards.thornBoar) forestWolf=$($f.Cards.forestWolf)" }
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
  Seed @("--name", $name, "--card", "meadowSlime:1")
}
function Expect-Paid([string]$who, [hashtable]$before, [hashtable]$after) {
  Ok "$who paid exactly 300 G" ($after.Gold -eq $before.Gold + 300) "$(Show $before) -> $(Show $after)"
  foreach ($id in $rewardCards) { Ok "$who got exactly 1 $id" ($after.Cards[$id] -eq $before.Cards[$id] + 1) "$(Show $before) -> $(Show $after)" }
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
  Ok "start: the quest was never taken, no reward cards" ($before.Quest -eq 0 -and $before.Cards.hornRabbit -eq 0) (Show $before)
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
  Ok "a second run (quest Done, practice duel) pays nothing" ($again.Gold -eq $after.Gold -and $again.Cards.hornRabbit -eq $after.Cards.hornRabbit -and $again.Cards.thornBoar -eq $after.Cards.thornBoar -and $again.Cards.forestWolf -eq $after.Cards.forestWolf -and $again.Quest -eq 3) "$(Show $after) -> $(Show $again)"

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
  Ok "after the kill: the quest is Active, nothing paid" ($mid.Quest -eq 1 -and $mid.Gold -eq $before.Gold -and $mid.Cards.hornRabbit -eq 0) (Show $mid)
  Ok "no zone exception for the dropped duel" (-not (Select-String -Path $script:zoneLog -Pattern "NullReference|Exception" -Quiet))
  $c = Start-Orin "drop-2" $drop
  Ok "the second login finished" (Finish $c 150)
  Lines $c
  Ok "the second login is gone" (Wait-Offline $drop 30)
  Expect-Paid "drop" $before (Facts $drop)

  Section "forfeit: giving up counts, paid once"
  $ff = "orq$suffix"
  Setup-Hero $ff "warrior"
  $before = Facts $ff
  $c = Start-Orin "forfeit-1" $ff @("--orin-forfeit")
  Ok "the bot finished" (Finish $c 150)
  Lines $c
  Ok "the bot is gone" (Wait-Offline $ff 30)
  Expect-Paid "forfeit" $before (Facts $ff)

  Section "no first card: Orin does not hand out the quest"
  $nc = "orn$suffix"
  Register-Account $nc "warrior"
  $c = Start-Orin "nocard-1" $nc @("--orin-peek")
  Ok "peek finished" (Finish $c 90)
  Lines $c
  Ok "no card, no quest in the API" ((Facts $nc).Quest -eq 0)
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
    Ok "paid once in the window run too" ((Facts $sh).Gold -eq 300)
  }
}
finally {
  foreach ($p in $script:clients) { Stop-Tree $p }
  Stop-Tree $script:zoneProc; Stop-Tree $script:apiProc; Stop-Tree $script:mongoProc
}
Write-Host ""
Write-Host "RESULT: $($script:passed) passed, $($script:failed) failed"
if ($script:failed -gt 0) { exit 1 }
