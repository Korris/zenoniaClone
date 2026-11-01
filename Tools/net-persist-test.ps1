<#
.SYNOPSIS
  End-to-end check of real accounts and saving on localhost: a real mongod (one-node replica set from server-api's dev-mongo, Docker not needed), the real server-api, a headless zone server and
  scripted clients (DebugCapture.NetPersist.cs) that log in with the real flow. Every number it judges comes from the API, not from the clients. Secrets are made up for this run and die with it.
  Register, create, join, earn gold, leave, rejoin; bad and reused tickets; a client that claims another class; one character twice; graceful and hard zone restarts; enhance through the API;
  trade then kill a client; kill during an open trade; kill the zone right after a trade; a trade and a leave during an API outage; the Oakhaven start; change character, log out, delete; what the
  account pages say for each refusal; forgot password; a zone stopped during an outage and its kept state coming back; a pet bought, worn, kept through relogin and zone restarts, traded. -Shots also opens a real window and saves a PNG of each account page. Prints PASS / FAIL lines and a count.
.EXAMPLE
  .\Tools\net-persist-test.ps1 -Exe C:\temp\login-build\Zenonia.exe -OutDir C:\temp\persist
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [string]$OutDir = "persist-test-out",
  [int]$ZonePort = 7795,
  [int]$ApiPort = 3195,
  [int]$MongoPort = 27195,
  [int]$SaveInterval = 5,
  [switch]$Shots,
  [switch]$PetsOnly # only the stack and the pet section; opens two real windows and saves pictures of them
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
$dbName = "persist_e2e"
$shutdownFile = "$OutDir\stop-zone"
$confirmFile = "$OutDir\confirm-now"
$suffix = Get-Random -Maximum 99999
$script:passed = 0; $script:failed = 0; $script:apiProc = $null; $script:zoneProc = $null; $script:mongoProc = $null; $script:clients = @(); $script:runs = 0

function Ok([string]$what, [bool]$cond, [string]$detail = "") {
  if ($cond) { $script:passed++; Write-Host "PASS $what" } else { $script:failed++; Write-Host "FAIL $what  $detail" }
}
function Section([string]$s) { Write-Host ""; Write-Host "=== $s" }

function Start-Mongo {
  $script:mongoProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("scripts/dev-mongo.mjs", "--port", $MongoPort, "--db-path", "`"$OutDir\mongo-data`"") -RedirectStandardOutput "$OutDir\mongo.out" -RedirectStandardError "$OutDir\mongo.err"
  $until = (Get-Date).AddSeconds(60)
  while ((Get-Date) -lt $until -and -not (Select-String -Path "$OutDir\mongo.out" -Pattern "READY" -Quiet -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 300 }
  if (-not (Select-String -Path "$OutDir\mongo.out" -Pattern "READY" -Quiet)) { throw "mongod did not start, see $OutDir\mongo.err" }
}
function Start-Api {
  $script:runs++
  $vars = @{ PORT = $ApiPort; HOST = "127.0.0.1"; MONGO_URI = $mongoUri; MONGO_DB = $dbName; JWT_SECRET = $jwtSecret; SERVICE_KEY = $serviceKey; ZONE_ID = "e2e-zone"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; ACCESS_TTL_SEC = 6; REGISTER_IP_MAX = 500; LOGIN_IP_MAX = 5000; BCRYPT_COST = 6 }
  foreach ($k in $vars.Keys) { Set-Item "env:$k" $vars[$k] }
  $script:apiProc = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api-$($script:runs).out" -RedirectStandardError "$OutDir\api-$($script:runs).err"
  foreach ($k in $vars.Keys) { Remove-Item "env:$k" }
  $until = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $until) { try { Invoke-RestMethod "$api/health" -TimeoutSec 2 | Out-Null; return } catch { Start-Sleep -Milliseconds 300 } }
  throw "server-api did not start, see $OutDir\api-$($script:runs).err"
}
function Start-Zone {
  $script:runs++
  Remove-Item $shutdownFile -ErrorAction SilentlyContinue
  $script:zoneLog = "$OutDir\zone-$($script:runs).log"
  $env:ZENONIA_SERVICE_KEY = $serviceKey
  # The cheats are there on purpose: they must have no effect on a real account.
  $script:zoneProc = Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList @("-batchmode", "-nographics", "--mode", "server", "--port", $ZonePort, "--api-url", $api, "--zone-id", "e2e-zone", "--save-interval", $SaveInterval, "--save-spool", "`"$OutDir\spool`"", "--shutdown-file", "`"$shutdownFile`"", "--dev-gold", 99999, "--dev-level", 60, "--dev-items", 5, "-logFile", "`"$($script:zoneLog)`"")
  Remove-Item env:ZENONIA_SERVICE_KEY
  if (-not (Wait-Log $script:zoneLog "zone sessions reset" 40)) { throw "the zone never got ready, see $($script:zoneLog)" }
}
function Stop-Tree($p) { if ($null -ne $p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; try { $p.WaitForExit(5000) | Out-Null } catch {} } }

function Wait-Log([string]$path, [string]$pattern, [int]$seconds) {
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) {
    if ((Test-Path $path) -and (Select-String -Path $path -Pattern $pattern -Quiet -ErrorAction SilentlyContinue)) { return $true }
    Start-Sleep -Milliseconds 300
  }
  return $false
}

# name = what the log file is called. Everything after it goes on the command line.
function Start-Client([string]$name, [string[]]$extra) {
  $log = "$OutDir\$name.log"
  $env:ZENONIA_TEST_PASSWORD = $password
  $p = Start-Process $Exe -PassThru -WindowStyle Hidden -ArgumentList (@("-batchmode", "-nographics", "--mode", "client", "-logFile", "`"$log`"") + $extra)
  Remove-Item env:ZENONIA_TEST_PASSWORD
  $script:clients += $p
  return @{ Proc = $p; Log = $log }
}
function Start-Account([string]$name, [string]$account, [string]$role, [string[]]$more = @()) {
  return Start-Client $name (@("--api-url", $api, "--account", $account, "--name", $account, "--net-bot", "persist", "--persist-role", $role) + $more)
}
function Show-Lines($c) { Select-String -Path $c.Log -Pattern "persist-e2e: (PASS|FAIL|player http|change character took)" -ErrorAction SilentlyContinue | ForEach-Object { $m = $_.Line -replace "^.*persist-e2e: ", ""; if ($m -match "^PASS") { $script:passed++ } elseif ($m -match "^FAIL") { $script:failed++ }; Write-Host $m } }
function Wait-Exit($c, [int]$seconds) { if (-not $c.Proc.WaitForExit($seconds * 1000)) { Stop-Tree $c.Proc; return $false }; return $true }
function Copy-Failures($c) { Select-String -Path $c.Log -Pattern "trade-e2e: FAIL|persist-e2e: FAIL|NullReference|Exception" -ErrorAction SilentlyContinue | ForEach-Object { $script:failed++; Write-Host ("FAIL in " + (Split-Path $c.Log -Leaf) + ": " + ($_.Line -replace "^\[ZenoniaProto\] ", "")) } }
function Line-Of($c, [string]$pattern) { (Select-String -Path $c.Log -Pattern $pattern -ErrorAction SilentlyContinue | Select-Object -Last 1).Line }
function Value-In([string]$line, [string]$key) { if ($line -match "$key=(-?\d+)") { return [int]$Matches[1] }; return -1 }

function Api-Call([string]$method, [string]$path, $body = $null, [string]$token = $null) {
  $h = @{}
  if ($token) { $h.Authorization = "Bearer $token" }
  $json = if ($null -ne $body) { $body | ConvertTo-Json -Depth 6 } elseif ($method -eq "POST") { "{}" } else { $null }
  try {
    $r = Invoke-WebRequest "$api$path" -Method $method -Headers $h -Body $json -ContentType "application/json" -UseBasicParsing -TimeoutSec 8
    return @{ Status = [int]$r.StatusCode; Body = ($r.Content | ConvertFrom-Json) }
  } catch {
    $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    return @{ Status = $status; Body = $null }
  }
}
function Register-Account([string]$name, [string]$class) {
  $r = Api-Call POST "/auth/register" @{ username = $name; email = "$name@example.com"; password = $password }
  if ($r.Status -ne 201) { throw "register $name -> $($r.Status)" }
  $token = $r.Body.accessToken
  $c = Api-Call POST "/characters" @{ name = $name; classId = $class } $token
  if ($c.Status -ne 201) { throw "create $name -> $($c.Status)" }
  return @{ Id = $c.Body.character.id; Name = $name }
}
function Get-Token([string]$name) { (Api-Call POST "/auth/login" @{ login = $name; password = $password }).Body.accessToken }
# The character as the API has it, through a fresh login: nothing cached anywhere.
function Read-Character([string]$name) {
  $t = Get-Token $name
  $list = (Api-Call GET "/characters" $null $t).Body.characters
  $id = @($list)[0].id
  return (Api-Call GET "/characters/$id" $null $t).Body.character
}
function Wait-Offline([string]$name, [int]$seconds) {
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) { if (-not (Read-Character $name).online) { return $true }; Start-Sleep -Milliseconds 700 }
  return $false
}
function Has-Item($char, [string]$uid) { return @($char.state.inventory | Where-Object { $_.uid -eq $uid }).Count }
function Svc-Call([string]$path, $body) {
  try {
    $r = Invoke-WebRequest "$api$path" -Method POST -Headers @{ "x-service-key" = $serviceKey } -Body ($body | ConvertTo-Json -Depth 12) -ContentType "application/json" -UseBasicParsing -TimeoutSec 8
    return @{ Status = [int]$r.StatusCode; Code = ""; Body = ($r.Content | ConvertFrom-Json) }
  } catch {
    $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    $code = ""; if ($_.ErrorDetails -and $_.ErrorDetails.Message) { try { $code = ($_.ErrorDetails.Message | ConvertFrom-Json).error.code } catch {} }
    return @{ Status = $status; Code = $code; Body = $null }
  }
}
function Start-Window([string]$name, [string]$account, [string]$role, [string[]]$more = @()) {
  $log = "$OutDir\$name.log"
  $env:ZENONIA_TEST_PASSWORD = $password
  $p = Start-Process $Exe -PassThru -ArgumentList (@("--mode", "client", "-screen-fullscreen", 0, "-screen-width", 960, "-screen-height", 640, "-logFile", "`"$log`"", "--api-url", $api, "--account", $account, "--name", $account, "--net-bot", "persist", "--persist-role", $role) + $more)
  Remove-Item env:ZENONIA_TEST_PASSWORD
  $script:clients += $p
  return @{ Proc = $p; Log = $log }
}
function Pet-Of($char) { return @($char.state.equipment | Where-Object { $_.slot -eq "pet" }) }
function All-Pets($char) { return @(@($char.state.equipment) + @($char.state.inventory) | Where-Object { $_.slot -eq "pet" }) }
function Seed([string[]]$a) { & node (Join-Path $apiDir "scripts\dev-seed.mjs") --uri $mongoUri --db $dbName @a | Out-Null }

try {
  Section "stack"
  Start-Mongo; Write-Host "mongod up (replica set, not Docker)"
  Start-Api; Write-Host "server-api up"
  Start-Zone; Write-Host "zone up, with dev cheats on its command line"
  Ok "the zone says the dev cheats are ignored" (Wait-Log $script:zoneLog "ignored with --api-url" 2)

  Section "pets: bought from the seller, worn, kept through relogin and restarts, traded"
  $pia = "pia$suffix"; $quin = "quin$suffix"
  Register-Account $pia "warrior" | Out-Null
  Register-Account $quin "wizard" | Out-Null
  Seed @("--name", $pia, "--gold", "10000", "--stones", "3")
  $c = Start-Account "pia-1-far" $pia "pet-far"
  Ok "the far round finished" (Wait-Exit $c 60)
  Show-Lines $c; Copy-Failures $c
  $spot = Line-Of $c "persist-e2e: spot "
  Ok "got a spot beside the seller" ($spot -match "x=([\d.]+) y=([\d.]+) map=(\w+)") $spot
  $sx = $Matches[1]; $sy = $Matches[2]; $smap = $Matches[3]
  Ok "left" (Wait-Offline $pia 25)
  $char = Read-Character $pia
  Ok "a stored character with no pet loads and saves as before" (@($char.state.equipment | Where-Object { $_.slot -eq "pet" }).Count -eq 0 -and $char.state.gold -eq 10000) "gold=$($char.state.gold)"
  Seed @("--name", $pia, "--x", $sx, "--y", $sy, "--map", $smap)
  $c = Start-Account "pia-2-buy" $pia "pet-buy"
  Ok "the buy round finished" (Wait-Exit $c 60)
  Show-Lines $c; Copy-Failures $c
  $line = Line-Of $c "persist-e2e: bought "
  Ok "left" (Wait-Offline $pia 25)
  $char = Read-Character $pia
  $worn = @($char.state.equipment | Where-Object { $_.slot -eq "pet" })
  $petUid = if ($worn.Count -eq 1) { $worn[0].uid } else { "" }
  $petHp = Value-In $line "maxhp"
  Ok "the API has the pet worn and the gold paid" ($worn.Count -eq 1 -and $worn[0].templateId -eq "pet-spirit" -and $worn[0].enhance -eq 0 -and $char.state.gold -eq 7000) "gold=$($char.state.gold) worn=$($worn | ConvertTo-Json -Compress)"
  Ok "the stones were not touched by the refused enhance" ($char.state.upgradeStone -eq 3) "stones=$($char.state.upgradeStone)"
  $c = Start-Account "pia-3-show" $pia "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  $line = Line-Of $c "persist-e2e: final"
  Ok "the pet survives leaving and rejoining, with its bonus" ($line -match "pet=$([regex]::Escape($petUid)) " -and (Value-In $line "maxhp") -eq $petHp) $line
  Ok "left" (Wait-Offline $pia 25)
  $c = Start-Account "pia-4-idle" $pia "idle"
  Ok "joined" (Wait-Log $c.Log "persist-e2e: idle" 40)
  $zone = $script:zoneProc
  New-Item -ItemType File -Path $shutdownFile | Out-Null
  Ok "the zone stops by itself" ($zone.WaitForExit(40000))
  Stop-Tree $c.Proc
  Start-Zone
  $c = Start-Account "pia-5-show" $pia "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  $line = Line-Of $c "persist-e2e: final"
  Ok "the pet survives a zone restart" ($line -match "pet=$([regex]::Escape($petUid)) " -and (Value-In $line "maxhp") -eq $petHp) $line
  Ok "left" (Wait-Offline $pia 25)
  $c = Start-Account "pia-6-imp" $pia "pet-buy-stay"
  Ok "bought the imp" (Wait-Log $c.Log "persist-e2e: imp-bought" 40)
  Show-Lines $c
  Start-Sleep -Seconds 1 # shorter than the save interval: only the save the purchase asks for can have happened
  Stop-Tree $script:zoneProc
  $char = Read-Character $pia
  $impUid = @($char.state.inventory | Where-Object { $_.templateId -eq "pet-imp" })[0].uid
  Ok "a killed zone keeps a purchase made a second ago: pet and gold together" ($null -ne $impUid -and $char.state.gold -eq 4000) "gold=$($char.state.gold)"
  Stop-Tree $c.Proc
  Start-Zone
  Seed @("--name", $pia, "--gold", "1000", "--pet-durability", "$($impUid):80") # a used pet: what the buyer must be told
  Seed @("--name", $quin, "--gold", "500", "--x", $sx, "--y", $sy, "--map", $smap)
  if ($PetsOnly) {
    $b = Start-Window "pia-7-trade" $pia "trade-a" @("--partner", $quin, "--give-uid", $impUid, "--trade-shots", "`"$OutDir\pet-shots`"")
    Start-Sleep -Seconds 1
    $k = Start-Window "quin-1-trade" $quin "trade-b" @("--partner", $pia, "--trade-shots", "`"$OutDir\pet-shots`"")
  } else {
    $b = Start-Account "pia-7-trade" $pia "trade-a" @("--partner", $quin, "--give-uid", $impUid)
    $k = Start-Account "quin-1-trade" $quin "trade-b" @("--partner", $pia)
  }
  Ok "the pet trade completed" ((Wait-Log $b.Log "persist-e2e: TRADE-DONE" 90) -and (Wait-Log $k.Log "persist-e2e: TRADE-DONE" 10))
  Copy-Failures $b; Copy-Failures $k
  $cb = Read-Character $pia; $cc = Read-Character $quin
  Ok "the API moved it: on the wizard, not on the warrior" ((Has-Item $cb $impUid) -eq 0 -and (Has-Item $cc $impUid) -eq 1)
  Ok "its durability went with it" (@($cc.state.inventory | Where-Object { $_.uid -eq $impUid })[0].durability -eq 80)
  Stop-Tree $b.Proc; Stop-Tree $k.Proc
  Ok "both left" ((Wait-Offline $pia 30) -and (Wait-Offline $quin 30))
  $k = Start-Account "quin-2-wear" $quin "pet-wear"
  Ok "the wear round finished" (Wait-Exit $k 60)
  Show-Lines $k; Copy-Failures $k
  Ok "left" (Wait-Offline $quin 25)
  Ok "the traded pet still has what it had" ((Value-In (Line-Of $k "persist-e2e: wearing") "petdur") -eq 80) (Line-Of $k "persist-e2e: wearing")
  Ok "no save was refused over a pet" (-not (Select-String -Path "$OutDir\zone-*.log" -Pattern "refused the state" -Quiet))
  if ($Shots -or $PetsOnly) {
    Section "pets: what another player sees, in real windows"
    $shotDir = "$OutDir\pet-shots"
    $env:ZENONIA_TEST_PASSWORD = $password
    $w1 = Start-Process $Exe -PassThru -ArgumentList @("--mode", "client", "-screen-fullscreen", 0, "-screen-width", 960, "-screen-height", 640, "-logFile", "`"$OutDir\pia-8-toggle.log`"", "--api-url", $api, "--account", $pia, "--name", $pia, "--net-bot", "persist", "--persist-role", "pet-toggle")
    $w2 = Start-Process $Exe -PassThru -ArgumentList @("--mode", "client", "-screen-fullscreen", 0, "-screen-width", 960, "-screen-height", 640, "-logFile", "`"$OutDir\quin-3-see.log`"", "--api-url", $api, "--account", $quin, "--name", $quin, "--net-bot", "persist", "--persist-role", "pet-see", "--partner", $pia, "--shot-path", "`"$shotDir`"")
    Remove-Item env:ZENONIA_TEST_PASSWORD
    $script:clients += $w1; $script:clients += $w2
    $c = @{ Proc = $w2; Log = "$OutDir\quin-3-see.log" }
    Ok "the watching window finished" (Wait-Exit $c 120)
    Show-Lines $c
    Show-Lines @{ Proc = $w1; Log = "$OutDir\pia-8-toggle.log" }
    Stop-Tree $w1
    Write-Host "see $shotDir"
    Ok "both left" ((Wait-Offline $pia 30) -and (Wait-Offline $quin 30))
  }

  Section "pet durability"
  $rae = "rae$suffix"
  Register-Account $rae "assassin" | Out-Null
  Seed @("--name", $rae, "--pet", "pet-spirit:rae-old-pet") # the shape a pet had before durability, in a state of the old format
  $c = Start-Account "rae-1-wear" $rae "pet-wear"
  Ok "the old pet round finished" (Wait-Exit $c 60)
  Show-Lines $c; Copy-Failures $c
  Ok "an old pet without the field loads at the maximum" ((Value-In (Line-Of $c "persist-e2e: wearing") "petdur") -eq 1000) (Line-Of $c "persist-e2e: wearing")
  Ok "left" (Wait-Offline $rae 25)
  Ok "and the API has it at the maximum too" ((Pet-Of (Read-Character $rae))[0].durability -eq 1000)

  Seed @("--name", $quin, "--pet-durability", "$($impUid):1000")
  $d0 = @(Pet-Of (Read-Character $pia))
  Ok "the warrior wears the spirit, the wizard the imp" ($d0.Count -eq 1 -and $d0[0].templateId -eq "pet-spirit" -and (Pet-Of (Read-Character $quin))[0].templateId -eq "pet-imp")
  $dBefore = $d0[0].durability
  # Straight to the fight: the portal bot picks its portal by name and not every one leads out from here. A blocked spot makes the zone use the entrance of the map.
  Seed @("--name", $pia, "--x", "1", "--y", "1", "--map", "frostHollow")
  Seed @("--name", $quin, "--x", "1", "--y", "1", "--map", "frostHollow")
  $a1 = Start-Account "pia-9-drain" $pia "pet-drain" @("--fight-seconds", "90")
  $a2 = Start-Account "quin-4-drain" $quin "pet-drain" @("--fight-seconds", "90")
  Ok "the fights finished" ((Wait-Exit $a1 200) -and (Wait-Exit $a2 200))
  Show-Lines $a1; Show-Lines $a2; Copy-Failures $a1; Copy-Failures $a2
  Select-String -Path $a1.Log, $a2.Log -Pattern "persist-e2e: rate" | ForEach-Object { Write-Host ($_.Line -replace "^.*persist-e2e: ", "") }
  Ok "both left" ((Wait-Offline $pia 30) -and (Wait-Offline $quin 30))
  $dApi = (Pet-Of (Read-Character $pia))[0].durability
  Ok "a monster hit drained the spirit, and the API has it" ($dApi -lt $dBefore) "api=$dApi before=$dBefore"
  $c = Start-Account "pia-10-show" $pia "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "durability survives relogin" ((Value-In (Line-Of $c "persist-e2e: final") "petdur") -eq $dApi) "$(Line-Of $c 'persist-e2e: final') api=$dApi"
  Ok "left" (Wait-Offline $pia 25)
  $c = Start-Account "pia-11-idle" $pia "idle"
  Ok "joined" (Wait-Log $c.Log "persist-e2e: idle" 40)
  $zone = $script:zoneProc
  New-Item -ItemType File -Path $shutdownFile | Out-Null
  Ok "the zone stops by itself" ($zone.WaitForExit(40000))
  Stop-Tree $c.Proc
  Start-Zone
  $c = Start-Account "pia-12-show" $pia "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "durability survives a zone restart" ((Value-In (Line-Of $c "persist-e2e: final") "petdur") -eq $dApi) (Line-Of $c "persist-e2e: final")
  Ok "left" (Wait-Offline $pia 25)

  # A save straight to the API, as a bad zone would send it.
  $t = Get-Token $quin
  $qid = (Read-Character $quin).id
  $enter = Api-Call POST "/characters/$qid/enter" $null $t
  $red = Svc-Call "/internal/tickets/redeem" @{ ticket = $enter.Body.ticket }
  $qs = $red.Body.character.state; $qrev = $red.Body.character.rev; $qsid = $red.Body.sessionId
  $qpet = @($qs.equipment | Where-Object { $_.slot -eq "pet" })[0]
  $stored = $qpet.durability
  Ok "the imp lost durability in the fight too" ($stored -lt 1000) "stored=$stored"
  $qpet.durability = $stored + 1
  $r = Svc-Call "/internal/characters/$qid/save" @{ sessionId = $qsid; rev = $qrev; state = $qs }
  Ok "a save above the stored durability is refused" ($r.Status -eq 422 -and $r.Code -eq "durability_tamper") "status $($r.Status) $($r.Code)"
  $qpet.durability = 1001
  $r = Svc-Call "/internal/characters/$qid/save" @{ sessionId = $qsid; rev = $qrev; state = $qs }
  Ok "a save over the maximum is refused" ($r.Status -eq 400) "status $($r.Status) $($r.Code)"
  Ok "and nothing changed" ((Pet-Of (Read-Character $quin))[0].durability -eq $stored)
  $r = Svc-Call "/internal/sessions/$qsid/end" @{ characterId = $qid }
  Ok "the session is given back" ($r.Status -eq 200) "status $($r.Status)"

  $petUid2 = (Pet-Of (Read-Character $pia))[0].uid
  Seed @("--name", $pia, "--pet-durability", "$($petUid2):4", "--x", "1", "--y", "1", "--map", "frostHollow")
  Seed @("--name", $quin, "--x", "1", "--y", "1", "--map", "frostHollow") # side by side at the entrance
  $shotDir = "$OutDir\pet-shots"
  if ($PetsOnly -or $Shots) {
    $w1 = Start-Window "pia-13-wearout" $pia "pet-wearout" @("--shot-path", "`"$shotDir`"")
    Start-Sleep -Seconds 1
    $w2 = Start-Window "quin-5-watch" $quin "pet-watch" @("--partner", $pia, "--shot-path", "`"$shotDir`"")
    Ok "the owner saw it wear out" (Wait-Log $w1.Log "persist-e2e: worn-out" 280)
    Ok "the other player's window finished" (Wait-Exit $w2 120)
    Show-Lines $w1; Show-Lines $w2
  } else {
    $w1 = Start-Account "pia-13-wearout" $pia "pet-wearout"
    Ok "the owner saw it wear out" (Wait-Log $w1.Log "persist-e2e: worn-out" 280)
    Show-Lines $w1
  }
  Ok "the zone logged no error for it" (-not (Select-String -Path $script:zoneLog -Pattern "NullReference|Exception" -Quiet))
  Start-Sleep -Seconds 1
  $char = Read-Character $pia
  Ok "the API has no spirit a second after it wore out, with the player online" (@(All-Pets $char).Count -eq 0) "pets=$((All-Pets $char) | ConvertTo-Json -Compress)"
  Stop-Tree $w1.Proc # gone without a goodbye right after
  Ok "left" (Wait-Offline $pia 30)
  $c = Start-Account "pia-14-show" $pia "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  $line = Line-Of $c "persist-e2e: final"
  Ok "it does not come back on reconnect" ($line -match "pet=- " -and $line -notmatch "pet-spirit") $line
  Ok "left" (Wait-Offline $pia 25)
  Ok "still none in the API" (@(All-Pets (Read-Character $pia)).Count -eq 0)

  $quinPet = (Pet-Of (Read-Character $quin))[0].uid
  Ok "the wizard is offline for this" (Wait-Offline $quin 30)
  Seed @("--name", $quin, "--pet-durability", "$($quinPet):2")
  $c = Start-Account "quin-6-wearout" $quin "pet-wearout"
  Ok "the imp wore out from attacks" (Wait-Log $c.Log "persist-e2e: worn-out" 280)
  Show-Lines $c
  Start-Sleep -Seconds 1 # shorter than the save interval: only the save the wear-out asks for can have happened
  Stop-Tree $script:zoneProc
  Ok "a zone killed a second after the wear-out has already written it" (@(All-Pets (Read-Character $quin)).Count -eq 0) "pets=$((All-Pets (Read-Character $quin)) | ConvertTo-Json -Compress)"
  Stop-Tree $c.Proc
  Start-Zone
  $c = Start-Account "quin-7-show" $quin "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "no pet after the restart either" ((Line-Of $c "persist-e2e: final") -match "pet=- ") (Line-Of $c "persist-e2e: final")
  Ok "left" (Wait-Offline $quin 25)

  if (-not $PetsOnly) {
  Section "bad tickets"
  $c = Start-Client "bad-short" @("--host", "127.0.0.1", "--port", $ZonePort, "--name", "Eve", "--class", "cleric", "--dev-key", "k", "--net-bot", "wander", "--run-seconds", "20")
  Ok "the dev key is no way in" (Wait-Log $c.Log "Rejected: Bad ticket" 25); Stop-Tree $c.Proc
  $c = Start-Client "bad-unknown" @("--host", "127.0.0.1", "--port", $ZonePort, "--name", "Eve", "--class", "cleric", "--dev-key", ("A" * 43), "--net-bot", "wander", "--run-seconds", "20")
  Ok "a well-formed unknown ticket is refused by the API" (Wait-Log $c.Log "Rejected: Bad or expired ticket" 25); Stop-Tree $c.Proc

  Section "register, create, join, earn, leave"
  $ann = "ann$suffix"
  $c = Start-Account "ann-1-earn" $ann "earn" @("--account-new", "--class", "warrior", "--account-wait", "9")
  Ok "registered, created and joined through the real flow" (Wait-Log $c.Log "persist-e2e: joined as" 60)
  Ok "a new hero begins in Oakhaven" (Wait-Log $c.Log "persist-e2e: loaded .* map=oakhavenVillage" 20) (Line-Of $c "persist-e2e: loaded")
  Ok "and can walk out of it to the fight" (Wait-Log $c.Log "PASS walked from oakhavenVillage" 70)
  Ok "the expired token was refreshed" (Wait-Log $c.Log "PASS the expired access token" 1)
  Ok "the bot earned gold in the fight" (Wait-Exit $c 320)
  Copy-Failures $c
  $earned = Value-In (Line-Of $c "persist-e2e: earned gold=") "gold"
  Ok "left the world: lock released" (Wait-Offline $ann 25)
  $char = Read-Character $ann
  Write-Host "client last saw gold=$earned; API has gold=$($char.state.gold) level=$($char.state.level) exp=$($char.state.exp) rev=$($char.rev) map=$($char.state.mapId)"
  Ok "the gold is saved" ($earned -gt 0 -and $char.state.gold -ge $earned) "api=$($char.state.gold) client=$earned"
  Ok "the dev cheats did nothing" ($char.state.gold -lt 99999 -and $char.level -lt 60 -and @($char.state.inventory).Count -lt 5)
  Ok "the position was saved" ($char.state.hasPosition -eq $true)
  $gold1 = $char.state.gold

  Section "rejoin"
  $c = Start-Account "ann-2-show" $ann "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  $line = Line-Of $c "persist-e2e: final"
  Ok "gold kept after leaving and rejoining" ((Value-In $line "gold") -eq $gold1) $line
  Ok "an existing hero stays on its own map, not Oakhaven" ($line -match "map=frostHollow") $line
  Ok "left again" (Wait-Offline $ann 25)

  Section "tickets: one use, one character once, the client's word is nothing"
  $t = Get-Token $ann
  $id = (Read-Character $ann).id
  $e = Api-Call POST "/characters/$id/enter" $null $t
  $ticket = $e.Body.ticket
  $c1 = Start-Client "ann-3-ticket" @("--host", "127.0.0.1", "--port", $ZonePort, "--name", "Hacker", "--class", "cleric", "--dev-key", $ticket, "--net-bot", "persist", "--persist-role", "idle")
  Ok "a real ticket joins" (Wait-Log $c1.Log "persist-e2e: idle" 40)
  $line = Line-Of $c1 "persist-e2e: idle"
  Ok "the client's class and name in the Hello meant nothing" ((Select-String -Path $script:zoneLog -Pattern "net: $ann \(Warrior\) joined" -Quiet) -and -not (Select-String -Path $script:zoneLog -Pattern "Hacker" -Quiet))
  Ok "gold is the saved gold" ((Value-In $line "gold") -eq $gold1) $line
  $c2 = Start-Client "ann-4-reuse" @("--host", "127.0.0.1", "--port", $ZonePort, "--name", "Twice", "--class", "warrior", "--dev-key", $ticket, "--net-bot", "wander", "--run-seconds", "20")
  Ok "the same ticket a second time is refused" (Wait-Log $c2.Log "Rejected: Bad or expired ticket" 25); Stop-Tree $c2.Proc
  $e2 = Api-Call POST "/characters/$id/enter" $null (Get-Token $ann)
  Ok "a second login of the same character gets no ticket while it is online" ($e2.Status -eq 409) "status $($e2.Status)"
  Stop-Tree $c1.Proc # hard kill: the server notices by timeout
  Ok "a killed client still gets saved and released" (Wait-Offline $ann 30)
  Ok "still the same gold" ((Read-Character $ann).state.gold -eq $gold1)

  Section "zone restart, graceful, with the player online"
  $c = Start-Account "ann-5-idle" $ann "idle"
  Ok "joined" (Wait-Log $c.Log "persist-e2e: idle" 40)
  $zone = $script:zoneProc
  New-Item -ItemType File -Path $shutdownFile | Out-Null
  Ok "the zone stops by itself after saving everybody" ($zone.WaitForExit(40000))
  Ok "it said so" (Select-String -Path $script:zoneLog -Pattern "shutdown complete" -Quiet)
  $char = Read-Character $ann
  Ok "the lock was released by the shutdown" (-not $char.online)
  Ok "gold kept" ($char.state.gold -eq $gold1)
  Stop-Tree $c.Proc
  Start-Zone
  $c = Start-Account "ann-6-show" $ann "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "gold kept after the zone restart" ((Value-In (Line-Of $c "persist-e2e: final") "gold") -eq $gold1)
  Ok "left" (Wait-Offline $ann 25)

  Section "zone killed, with the player earning"
  $c = Start-Account "ann-7-earn" $ann "earn-stay"
  Ok "earned more" (Wait-Log $c.Log "persist-e2e: earned gold=" 320)
  $earned = Value-In (Line-Of $c "persist-e2e: earned gold=") "gold"
  Start-Sleep -Seconds ($SaveInterval * 2 + 1) # the periodic save has to have passed
  Stop-Tree $script:zoneProc
  $char = Read-Character $ann
  Write-Host "client saw gold=$earned before the kill; API has $($char.state.gold)"
  Ok "a killed zone loses at most the last save interval: the gold is there" ($char.state.gold -ge $earned) "api=$($char.state.gold)"
  Stop-Tree $c.Proc
  Start-Zone
  Ok "the restarted zone freed the lock at once" (-not (Read-Character $ann).online)
  $gold2 = $char.state.gold
  $c = Start-Account "ann-8-show" $ann "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "and the character plays on from there" ((Value-In (Line-Of $c "persist-e2e: final") "gold") -eq $gold2)
  Ok "left" (Wait-Offline $ann 25)

  Section "enhance goes through the API"
  Seed @("--name", $ann, "--stones", "3")
  $c = Start-Account "ann-9-enhance" $ann "enhance"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "left" (Wait-Offline $ann 25)
  $char = Read-Character $ann
  $w = @($char.state.equipment | Where-Object { $_.slot -eq "weapon" })[0]
  Ok "the stone is spent and the level saved" ($char.state.upgradeStone -eq 2 -and $w.enhance -eq 1) "stones=$($char.state.upgradeStone) +$($w.enhance)"
  Ok "no save was refused for it" (-not (Select-String -Path $script:zoneLog -Pattern "enhance_tamper" -Quiet))

  Section "trade, then kill one client"
  $bob = "bob$suffix"; $cat = "cat$suffix"; $uid = "leatherVest-e2e$suffix"
  Register-Account $bob "warrior" | Out-Null
  Register-Account $cat "wizard" | Out-Null
  Seed @("--name", $bob, "--gold", "1000", "--item", "leatherVest:$uid")
  Seed @("--name", $cat, "--gold", "500")
  $b = Start-Account "bob-1-trade" $bob "trade-a" @("--partner", $cat, "--give-uid", $uid)
  $k = Start-Account "cat-1-trade" $cat "trade-b" @("--partner", $bob)
  Ok "the trade completed for Bob" (Wait-Log $b.Log "persist-e2e: TRADE-DONE" 90)
  Ok "the trade completed for Cat" (Wait-Log $k.Log "persist-e2e: TRADE-DONE" 10)
  Copy-Failures $b; Copy-Failures $k
  Stop-Tree $k.Proc # gone without a goodbye, the moment it is told
  $cb = Read-Character $bob; $cc = Read-Character $cat
  Ok "durable before anybody left: Bob 1000 -300 +100" ($cb.state.gold -eq 800) "gold=$($cb.state.gold)"
  Ok "durable before anybody left: Cat 500 -100 +300" ($cc.state.gold -eq 700) "gold=$($cc.state.gold)"
  Ok "the item is on Cat and not on Bob" ((Has-Item $cb $uid) -eq 0 -and (Has-Item $cc $uid) -eq 1)
  Ok "Cat's timeout saves and releases" (Wait-Offline $cat 30)
  Stop-Tree $b.Proc
  Ok "Bob too" (Wait-Offline $bob 30)
  $cb = Read-Character $bob; $cc = Read-Character $cat
  Ok "after both left: gold still adds up to 1500" ($cb.state.gold + $cc.state.gold -eq 1500) "$($cb.state.gold) + $($cc.state.gold)"
  Ok "after both left: exactly one copy of the item" ((Has-Item $cb $uid) + (Has-Item $cc $uid) -eq 1 -and (Has-Item $cc $uid) -eq 1)
  $k = Start-Account "cat-2-show" $cat "show"
  Wait-Exit $k 60 | Out-Null; Copy-Failures $k
  $line = Line-Of $k "persist-e2e: final"
  Ok "Cat rejoins with it" ((Value-In $line "gold") -eq 700 -and $line -match [regex]::Escape($uid)) $line
  Ok "left" (Wait-Offline $cat 25)

  Section "kill a client with the trade locked, not confirmed"
  $b = Start-Account "bob-2-hang" $bob "hang-b" @("--partner", $cat)
  $k = Start-Account "cat-3-hang" $cat "hang-a" @("--partner", $bob, "--give-uid", $uid)
  Ok "both locked" ((Wait-Log $b.Log "persist-e2e: window stays open" 90) -and (Wait-Log $k.Log "persist-e2e: window stays open" 10))
  Stop-Tree $k.Proc
  Ok "Cat released" (Wait-Offline $cat 30)
  Stop-Tree $b.Proc
  Ok "Bob released" (Wait-Offline $bob 30)
  $cb = Read-Character $bob; $cc = Read-Character $cat
  Ok "nothing moved" ($cb.state.gold -eq 800 -and $cc.state.gold -eq 700 -and (Has-Item $cc $uid) -eq 1 -and (Has-Item $cb $uid) -eq 0) "bob=$($cb.state.gold) cat=$($cc.state.gold)"

  Section "kill the zone right after a trade"
  $k = Start-Account "cat-4-trade" $cat "trade-a" @("--partner", $bob, "--give-uid", $uid)
  $b = Start-Account "bob-3-trade" $bob "trade-b" @("--partner", $cat)
  Ok "traded" ((Wait-Log $k.Log "persist-e2e: TRADE-DONE" 90))
  Stop-Tree $script:zoneProc
  Copy-Failures $k; Copy-Failures $b
  $cb = Read-Character $bob; $cc = Read-Character $cat
  Ok "the trade outlived the zone: gold" ($cc.state.gold -eq 500 -and $cb.state.gold -eq 1000) "cat=$($cc.state.gold) bob=$($cb.state.gold)"
  Ok "the trade outlived the zone: one copy, on Bob" ((Has-Item $cb $uid) -eq 1 -and (Has-Item $cc $uid) -eq 0)
  Stop-Tree $k.Proc; Stop-Tree $b.Proc
  Start-Zone

  Section "API down: a trade is refused, nothing is lost, saves catch up"
  $b = Start-Account "bob-4-outage" $bob "trade-a" @("--partner", $cat, "--give-uid", $uid, "--confirm-file", "`"$confirmFile`"", "--expect-refused")
  $k = Start-Account "cat-5-outage" $cat "trade-b" @("--partner", $bob, "--confirm-file", "`"$confirmFile`"", "--expect-refused")
  Ok "both locked" ((Wait-Log $b.Log "persist-e2e: waiting to confirm" 90) -and (Wait-Log $k.Log "persist-e2e: waiting to confirm" 10))
  $revBefore = (Read-Character $bob).rev
  Stop-Tree $script:apiProc
  Start-Sleep -Seconds ($SaveInterval + 12) # the next periodic save fails and the zone knows
  New-Item -ItemType File -Path $confirmFile | Out-Null
  Ok "the trade was refused while the API is down" ((Wait-Log $b.Log "persist-e2e: REFUSED" 25) -and (Wait-Log $k.Log "persist-e2e: REFUSED" 5))
  Copy-Failures $b; Copy-Failures $k
  Stop-Tree $k.Proc # leaves during the outage: its last save has nowhere to go yet
  Start-Sleep -Seconds 12
  Start-Api
  Ok "a player who left during the outage is written and released once the API is back" (Wait-Offline $cat 70)
  $cc = Read-Character $cat; $cb = Read-Character $bob
  Ok "saves went on after the outage" ($cb.rev -gt $revBefore) "rev $revBefore -> $($cb.rev)"
  Ok "nothing moved" ($cc.state.gold -eq 500 -and $cb.state.gold -eq 1000 -and (Has-Item $cb $uid) -eq 1 -and (Has-Item $cc $uid) -eq 0) "cat=$($cc.state.gold) bob=$($cb.state.gold)"
  Ok "no player's state had to go to the spool" (-not (Test-Path "$OutDir\spool") -or @(Get-ChildItem "$OutDir\spool").Count -eq 0)

  Section "new heroes start in Oakhaven, change character, log out, delete"
  $dee = "dee$suffix"
  $c = Start-Client "dee-1-actions" @("--api-url", $api, "--account", $dee, "--account-new", "--name", $dee, "--class", "warrior", "--net-bot", "persist", "--persist-role", "actions", "--test-token-store")
  Ok "the whole round finished" (Wait-Exit $c 240)
  Show-Lines $c
  Ok "it said it was done" (Wait-Log $c.Log "persist-e2e: actions done" 1)
  $t = Get-Token $dee
  $left = @((Api-Call GET "/characters" $null $t).Body.characters)
  Ok "the API agrees: one hero left, the warrior, offline" ($left.Count -eq 1 -and $left[0].classId -eq "warrior" -and -not $left[0].online) ($left | ConvertTo-Json -Compress)
  $deeWiz = Api-Call POST "/characters" @{ name = "$($dee)w"; classId = "wizard" } $t
  Ok "the deleted hero's name and class are free again, and it starts in Oakhaven" ($deeWiz.Status -eq 201 -and $deeWiz.Body.character.state.mapId -eq "oakhavenVillage") "status $($deeWiz.Status)"

  Section "deleting a hero that is in the world"
  $eve = "eve$suffix"
  $c = Start-Account "eve-1-idle" $eve "idle" @("--account-new", "--class", "cleric")
  Ok "joined" (Wait-Log $c.Log "persist-e2e: idle" 60)
  $k = Start-Client "eve-2-delete" @("--api-url", $api, "--account", $eve, "--name", $eve, "--net-bot", "persist", "--account-script", "delete-online", "--delete", $eve)
  Ok "the delete script ran" (Wait-Exit $k 60)
  Show-Lines $k
  $evec = Read-Character $eve
  Ok "the API still has it" ($null -ne $evec -and $evec.name -eq $eve)
  Stop-Tree $c.Proc
  Ok "released" (Wait-Offline $eve 30)

  Section "what the account pages say, forgot password"
  $mia = "mia$suffix"
  Register-Account $mia "warrior" | Out-Null
  $r1 = Api-Call POST "/auth/password-reset/request" @{ email = "$mia@example.com" }
  $r2 = Api-Call POST "/auth/password-reset/request" @{ email = "nobody-$mia@example.com" }
  Ok "the API answers a known and an unknown email the same" ($r1.Status -eq 202 -and $r2.Status -eq 202 -and ($r1.Body | ConvertTo-Json -Compress) -eq ($r2.Body | ConvertTo-Json -Compress)) "$($r1.Status) $($r2.Status)"
  # A second server-api on the same data with a tiny IP limit, for the 429 of the reset call.
  $limitedPort = $ApiPort + 1
  $vars = @{ PORT = $limitedPort; HOST = "127.0.0.1"; MONGO_URI = $mongoUri; MONGO_DB = $dbName; JWT_SECRET = $jwtSecret; SERVICE_KEY = $serviceKey; ZONE_ID = "e2e-zone-limited"; ZONE_HOST = "127.0.0.1"; ZONE_PORT = $ZonePort; LOGIN_IP_MAX = 2; BCRYPT_COST = 6 }
  foreach ($k2 in $vars.Keys) { Set-Item "env:$k2" $vars[$k2] }
  $limited = Start-Process node -PassThru -WindowStyle Hidden -WorkingDirectory $apiDir -ArgumentList @("src/server.js") -RedirectStandardOutput "$OutDir\api-limited.out" -RedirectStandardError "$OutDir\api-limited.err"
  foreach ($k2 in $vars.Keys) { Remove-Item "env:$k2" }
  $script:clients += $limited
  $until = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $until) { try { Invoke-RestMethod "http://127.0.0.1:$limitedPort/health" -TimeoutSec 2 | Out-Null; break } catch { Start-Sleep -Milliseconds 300 } }
  $c = Start-Client "mia-1-messages" @("--api-url", $api, "--limited-api", "http://127.0.0.1:$limitedPort", "--account", $mia, "--name", $mia, "--net-bot", "persist", "--account-script", "messages")
  Ok "the messages script ran" (Wait-Exit $c 120)
  Show-Lines $c
  Ok "it said it was done" (Wait-Log $c.Log "persist-e2e: script done" 1)
  Stop-Tree $limited

  if ($Shots) {
    Section "pictures of the account pages, in a real window"
    $sid = "sid$suffix"
    $dir = "$OutDir\shots"
    $env:ZENONIA_TEST_PASSWORD = $password
    $p = Start-Process $Exe -PassThru -ArgumentList @("--mode", "client", "-screen-fullscreen", 0, "-screen-width", 1138, "-screen-height", 640, "-logFile", "`"$OutDir\sid-shots.log`"", "--api-url", $api, "--account", $sid, "--name", $sid, "--login-shots", "`"$dir`"")
    Remove-Item env:ZENONIA_TEST_PASSWORD
    $script:clients += $p
    $c = @{ Proc = $p; Log = "$OutDir\sid-shots.log" }
    Ok "the shots finished" (Wait-Exit $c 120)
    Show-Lines $c
    Write-Host "see $dir"
  }

  Section "secrets in the logs"
  $allLogs = Get-ChildItem $OutDir -File | Where-Object { $_.Extension -in ".log", ".out", ".err" }
  Ok "no password in any log" (@($allLogs | Select-String -SimpleMatch $password).Count -eq 0)
  Ok "no service key in any log" (@($allLogs | Select-String -SimpleMatch $serviceKey).Count -eq 0)
  Ok "no exceptions in the zone logs" (@(Get-ChildItem $OutDir -Filter "zone-*.log" | Select-String -Pattern "NullReference|Exception").Count -eq 0)
  Ok "the API logged no errors" (@(Get-ChildItem $OutDir -Filter "api-*.out" | Select-String -SimpleMatch '"level":50').Count -eq 0)

  # Last on purpose: this one writes to the spool, which the checks above look at.
  Section "API down through a zone stop: the state waits on disk and comes back at the next login"
  Stop-Tree $b.Proc # Bob has been standing there since the outage above: this is about one player
  Ok "Bob has left" (Wait-Offline $bob 30)
  $c = Start-Account "ann-10-spool" $ann "earn-stay"
  Ok "joined" (Wait-Log $c.Log "persist-e2e: loaded" 40)
  $goldApi = (Read-Character $ann).state.gold
  $spoolZone = $script:zoneProc
  Stop-Tree $script:apiProc
  Ok "earned with the API away" (Wait-Log $c.Log "persist-e2e: earned gold=" 320)
  $earned = Value-In (Line-Of $c "persist-e2e: earned gold=") "gold"
  New-Item -ItemType File -Path $shutdownFile | Out-Null
  Ok "the zone stops by itself after its 30 s of trying" ($spoolZone.WaitForExit(60000))
  Stop-Tree $c.Proc
  $kept = @(Get-ChildItem "$OutDir\spool" -Filter "*.json" -ErrorAction SilentlyContinue)
  Ok "one state was kept on disk" ($kept.Count -eq 1) "$($kept.Count) files"
  $keptGold = (Get-Content $kept[0].FullName -Raw | ConvertFrom-Json).state.gold # the bot goes on fighting after it reports, so this can be more than $earned
  Ok "with everything earned in it" ($keptGold -ge $earned) "kept=$keptGold earned=$earned"
  Start-Api
  $id = (Read-Character $ann).id
  Ok "the API has only the older gold, so the file matters" ((Read-Character $ann).state.gold -lt $keptGold) "api=$((Read-Character $ann).state.gold) kept=$keptGold before=$goldApi"
  Start-Zone
  Ok "the next zone says it is waiting" (Select-String -Path $script:zoneLog -Pattern "could not be written and wait" -Quiet)
  $c = Start-Account "ann-11-back" $ann "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  $line = Line-Of $c "persist-e2e: final"
  Ok "the hero has what it earned during the outage" ((Value-In $line "gold") -eq $keptGold) "$line kept=$keptGold"
  Ok "left" (Wait-Offline $ann 25)
  Ok "the API has it now" ((Read-Character $ann).state.gold -eq $keptGold)
  Ok "the file is done with" (@(Get-ChildItem "$OutDir\spool" -Filter "*.json").Count -eq 0 -and @(Get-ChildItem "$OutDir\spool" -Filter "*.reloaded").Count -eq 1)

  $rev = (Read-Character $ann).rev
  $old = Get-Content ($kept[0].FullName + ".reloaded") -Raw | ConvertFrom-Json
  $old.rev = $rev - 1; $old.state.gold = 987654
  $path = "$OutDir\spool\$id-20990101000000000.json"
  [IO.File]::WriteAllText($path, ($old | ConvertTo-Json -Depth 12))
  $c = Start-Account "ann-12-stale" $ann "show"
  Wait-Exit $c 60 | Out-Null; Copy-Failures $c
  Ok "a kept state from before the last write is not used" ((Value-In (Line-Of $c "persist-e2e: final") "gold") -eq $keptGold) (Line-Of $c "persist-e2e: final")
  Ok "and stays for a person" ((Test-Path $path) -and (Select-String -Path $script:zoneLog -Pattern "is not used: built on rev" -Quiet))
  Ok "left" (Wait-Offline $ann 25)
  } # not -PetsOnly
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
