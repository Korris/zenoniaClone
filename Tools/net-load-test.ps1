<#
.SYNOPSIS
  Headless load test of one zone: a dedicated server (-batchmode -nographics) against N protocol bots (Tests/dotnet/ZenoniaSwarm, one process, no
  Unity, no windows). Ramps through the given client counts (a fresh server per step), logs the server's memory, and checks the budget:
  tick p99 < 33 ms, nobody disconnected or rejected, average download <= 24 KB/s per client.

.EXAMPLE
  .\Tools\net-load-test.ps1 -Exe C:\path\Zenonia.exe -Steps 50,100,200 -StepSeconds 60 -OutDir C:\temp\load
  .\Tools\net-load-test.ps1 -Exe ... -Steps 200 -StepSeconds 600 -OutDir C:\temp\soak          # the 10 minute soak
  .\Tools\net-load-test.ps1 -Exe ... -Steps 200 -StepSeconds 60 -NoDelta -OutDir C:\temp\full   # same load with full snapshots only (comparison)
  .\Tools\net-load-test.ps1 -Exe ... -Steps 200 -StepSeconds 60 -Portals -OutDir C:\temp\spread  # wanderers may leave through portals and the bots spread over the zones
  The server hosts every map, but by default it runs --no-portals: all bots stay on Frost Hollow, which is the worst case the budget was set for (the other zones sit paused).
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [int[]]$Steps = @(50, 100, 200),
  [int]$StepSeconds = 60,
  [string]$OutDir = "net-load-out",
  [int]$Port = 7800,
  [string]$Mix = "50:30:20",          # fighters : wanderers : idlers
  [int]$SimLatency = 0, [int]$SimJitter = 0, [int]$SimLoss = 0,
  [switch]$NoDelta,
  [switch]$Portals
)
$ErrorActionPreference = "Stop"
$Exe = (Resolve-Path $Exe).Path
$repo = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$swarmProj = Join-Path $repo "Tests\dotnet\ZenoniaSwarm\ZenoniaSwarm.csproj"
dotnet build $swarmProj -c Release --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "swarm build failed" }
$swarmDll = Get-ChildItem (Join-Path $repo "Tests\dotnet\ZenoniaSwarm\bin\Release") -Recurse -Filter "ZenoniaSwarm.dll" | Select-Object -First 1
$allOk = $true
$summary = @()

foreach ($n in $Steps) {
  $dir = Join-Path $OutDir "n$n"
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  Remove-Item "$dir\*" -Force -ErrorAction SilentlyContinue
  $serverSeconds = $StepSeconds + 20 + [int]($n * 0.05)
  $serverArgs = @("-batchmode", "-nographics", "--mode", "server", "--port", $Port, "--dev-key", "k", "--max-players", ($n + 5), "--run-seconds", $serverSeconds,
    "--net-stats", "$dir\server.json", "-logFile", "$dir\server.log")
  if ($NoDelta) { $serverArgs += "--net-no-delta" }
  if (-not $Portals) { $serverArgs += "--no-portals" }
  $server = Start-Process -FilePath $Exe -ArgumentList $serverArgs -PassThru -WindowStyle Hidden
  $deadline = (Get-Date).AddSeconds(40)
  while ((Get-Date) -lt $deadline) {
    if ((Test-Path "$dir\server.log") -and (Select-String -Path "$dir\server.log" -Pattern "listening on UDP" -Quiet -ErrorAction SilentlyContinue)) { break }
    Start-Sleep -Milliseconds 300
  }
  $swarmArgs = @($swarmDll.FullName, "--host", "127.0.0.1", "--port", $Port, "--key", "k", "--clients", $n, "--seconds", $StepSeconds, "--stagger-ms", 40, "--mix", $Mix, "--stats", "$dir\swarm.json")
  if ($SimLatency -gt 0 -or $SimLoss -gt 0) { $swarmArgs += @("--sim-latency", $SimLatency, "--sim-jitter", $SimJitter, "--sim-loss", $SimLoss) }
  $swarm = Start-Process -FilePath "dotnet" -ArgumentList $swarmArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$dir\swarm.txt"
  # server memory every 5 s while the swarm runs
  $mem = @()
  while (-not $swarm.HasExited) {
    $server.Refresh()
    if (-not $server.HasExited) { $mem += [math]::Round($server.WorkingSet64 / 1MB, 1) }
    Start-Sleep -Seconds 5
  }
  $mem | Out-File "$dir\server-memory-mb.txt"
  if (-not $server.WaitForExit(90000)) { Write-Warning "server did not exit, killing"; $server.Kill() }

  $s = Get-Content "$dir\server.json" -Raw | ConvertFrom-Json
  $w = Get-Content "$dir\swarm.json" -Raw | ConvertFrom-Json
  $tickP99 = [double]$s.tickMs.p99
  $ok = ($tickP99 -lt 33) -and ($w.lostConnection -eq 0) -and ($w.rejected -eq 0) -and ($w.downKBps.avg -le 24) -and ($w.receivingSnapshots -eq $n)
  if (-not $ok) { $allOk = $false }
  $peakMem = if ($mem.Count) { ($mem | Measure-Object -Maximum).Maximum } else { 0 }
  $row = [pscustomobject]@{
    Clients = $n; TickAvgMs = [math]::Round([double]$s.tickMs.avg, 3); TickP99Ms = [math]::Round($tickP99, 3); TickMaxMs = [math]::Round([double]$s.tickMs.max, 1)
    DownAvgKBps = $w.downKBps.avg; DownP95 = $w.downKBps.p95; UpAvgKBps = $w.upKBps.avg; SnapAvgB = $w.snapshotAvgBytes; SnapMaxB = $w.snapshotMaxBytes
    FullSnap = "$($w.snapshotsFull)/$($w.snapshotsTotal)"; Lost = $w.lostConnection; Rejected = $w.rejected; ServerMemPeakMB = $peakMem; Exceptions = [int]$s.exceptions; Pass = $ok
  }
  $summary += $row
  Write-Host ("[{0} bots] tick avg {1} p99 {2} max {3} ms | down {4} up {5} KB/s | snapshot avg {6} max {7} B | lost {8} | server RAM peak {9} MB | {10}" -f $n, $row.TickAvgMs, $row.TickP99Ms, $row.TickMaxMs, $row.DownAvgKBps, $row.UpAvgKBps, $row.SnapAvgB, $row.SnapMaxB, $row.Lost, $row.ServerMemPeakMB, $(if ($ok) { "PASS" } else { "FAIL" }))
}
$summary | ConvertTo-Json | Out-File (Join-Path $OutDir "load-summary.json")
$summary | Format-Table -AutoSize
if ($allOk) { Write-Host "LOAD TEST PASSED" } else { Write-Host "LOAD TEST FAILED" }
exit $(if ($allOk) { 0 } else { 1 })
