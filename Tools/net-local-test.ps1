<#
.SYNOPSIS
  Localhost multi-process test of the online world: one headless zone server + N unattended bot clients (all -batchmode -nographics, so no
  windows pop up), optional ONE small visible client that records a video of the fight, then the budget check.

.EXAMPLE
  .\Tools\net-local-test.ps1 -Exe C:\path\Zenonia.exe -Clients 8 -Seconds 90 -OutDir C:\temp\net8
  .\Tools\net-local-test.ps1 -Exe ... -Clients 5 -Seconds 80 -VideoDir C:\temp\frames   # + one 640x400 recording client
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [int]$Clients = 8,
  [int]$Seconds = 90,                 # run time of every bot client
  [int]$Port = 7790,
  [string]$OutDir = "net-test-out",
  [ValidateSet("boss", "fight", "wander", "twitch", "skills")][string]$Bot = "boss",   # skills = fight + learn every skill online + cycle the skill keys (server gets --dev-level)
  [ValidateSet("boss", "fight", "wander", "twitch", "skills")][string]$CamBot = "boss",   # brain of the visible recording client
  [string]$DevKey = "k",
  [int]$MaxPlayers = 200,
  [string]$VideoDir = "",             # set = one extra visible client records frames here
  [int]$VideoSeconds = 12,
  [int]$VideoLead = 0,                # seconds after joining before the recording starts
  [int]$BotDelay = 0,                 # every bot waits this long after joining before it moves (so the whole party sets off together)
  [int]$SimLatency = 0,               # lag harness (bot clients only): one-way ms each direction, +-jitter ms, loss percent each direction
  [int]$SimJitter = 0,
  [int]$SimLoss = 0,
  [string[]]$ClientArgs = @(),        # extra command line for every bot client, e.g. --no-predict
  [string[]]$CamArgs = @(),           # extra command line for the visible recording client (sim options are NOT applied to it unless added here)
  [int]$TwitchBots = 0,               # the LAST n bot clients run the "twitch" brain (run / stop / swing in place): input-latency probe, stresses the prediction
  [int]$ToleranceMs = 200,            # boss-death sync window of the checker (widen it for lag runs)
  [string[]]$ServerArgs = @(),        # extra command line for the server, e.g. --net-no-delta
  [int]$DevLevel = 0,                 # server --dev-level: new heroes start at this level (the skills bot sets 60 itself)
  [switch]$NoCheck
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
Remove-Item "$OutDir\*" -Force -Recurse -ErrorAction SilentlyContinue
$classes = "warrior", "wizard", "mechanic", "assassin", "cleric"
$procs = @()

function Start-Unity([string]$name, [string[]]$arguments, [string]$style = "Hidden") {
  # a hidden window does not render (the recording client needs a real, small, visible one); the headless processes never create a window
  $p = Start-Process -FilePath $Exe -ArgumentList $arguments -PassThru -WindowStyle $style
  Write-Host ("started {0} (pid {1})" -f $name, $p.Id)
  return $p
}

# --- server ---
$serverSeconds = $Seconds + 30 + [int]($Clients * 0.25)
if ($DevLevel -eq 0 -and ($Bot -eq "skills" -or $CamBot -eq "skills")) { $DevLevel = 60 }
if ($DevLevel -gt 0) { $ServerArgs += @("--dev-level", $DevLevel) }
$server = Start-Unity "server" (@("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", $DevKey, "--max-players", $MaxPlayers,
  "--run-seconds", $serverSeconds, "--net-stats", "$OutDir\server.json", "-logFile", "$OutDir\server.log") + $ServerArgs)
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline) {
  if ((Test-Path "$OutDir\server.log") -and (Select-String -Path "$OutDir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue)) { break }
  Start-Sleep -Milliseconds 300
}

# --- one visible recording client (small window) ---
if ($VideoDir -ne "") {
  New-Item -ItemType Directory -Force -Path $VideoDir | Out-Null
  $procs += Start-Unity "camera client" (@("-screen-width", 640, "-screen-height", 400, "-screen-fullscreen", 0, "--mode", "client", "--host", "127.0.0.1", "--port", $Port,
    "--name", "Cam", "--class", "warrior", "--dev-key", $DevKey, "--net-bot", $CamBot, "--bot-delay", $BotDelay, "--record-net", $VideoSeconds, "--record-lead", $VideoLead,
    "--shot-path", $VideoDir, "-logFile", "$OutDir\camera.log") + $CamArgs) "Normal"
}

if ($VideoDir -ne "") { Start-Sleep -Seconds 4 }  # the camera client is in the world before the bots arrive

# --- bot clients (headless) ---
$sim = @()
if ($SimLatency -gt 0 -or $SimLoss -gt 0) { $sim = @("--sim-latency", $SimLatency, "--sim-jitter", $SimJitter, "--sim-loss", $SimLoss, "--sim-seed", 7) }
for ($i = 1; $i -le $Clients; $i++) {
  $cls = $classes[($i - 1) % $classes.Count]
  $procs += Start-Unity "bot$i ($cls)" (@("-batchmode", "-nographics", "--mode", "client", "--host", "127.0.0.1", "--port", $Port, "--name", "Bot$i", "--class", $cls,
    "--dev-key", $DevKey, "--net-bot", $(if ($i -gt $Clients - $TwitchBots) { "twitch" } else { $Bot }), "--bot-delay", $BotDelay, "--run-seconds", $Seconds, "--net-stats", "$OutDir\c$i.json", "-logFile", "$OutDir\c$i.log") + $sim + $ClientArgs)
  Start-Sleep -Milliseconds 250
}

# --- wait ---
$limit = (Get-Date).AddSeconds($Seconds + 90)
foreach ($p in $procs) {
  $left = [int]([math]::Max(1, ($limit - (Get-Date)).TotalSeconds))
  if (-not $p.WaitForExit($left * 1000)) { Write-Warning "client pid $($p.Id) did not exit in time, killing"; $p.Kill() }
}
if (-not $server.WaitForExit(60000)) { Write-Warning "server did not exit in time, killing"; $server.Kill() }

if ($NoCheck) { exit 0 }
$checker = Join-Path $PSScriptRoot "net-check-summary.mjs"
node $checker $OutDir --clients $Clients --tolerance-ms $ToleranceMs
exit $LASTEXITCODE
