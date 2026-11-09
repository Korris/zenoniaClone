$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $repo 'Build/Avatar-Hair-Frames/Zenonia.exe'
$out=Join-Path $repo 'Build/Avatar-Hair-Frames/captures'
New-Item -ItemType Directory -Force $out | Out-Null
foreach ($pair in @(@('mechanic','warrior'),@('assassin','cleric'))) {
  $jobs=@()
  foreach ($cls in $pair) {
    $captureArgs=@('--class',$cls,'--level','60','--worn-tier','0','--wear-tiers','0,0,0','--face','down','--hairstyle','1','--bot','attack','--god','--offscreen-capture','--zoom','42','--tp','40,30','--shots','1.0','--shot-path',"$out/$cls",'-screen-width','900','-screen-height','900','-screen-fullscreen','0','-logFile',"$out/$cls.log")
    $jobs+=@{Name=$cls;Process=(Start-Process $exe -ArgumentList $captureArgs -WindowStyle Hidden -PassThru)}
  }
  foreach ($job in $jobs) {
    if (-not $job.Process.WaitForExit(45000)) {throw "Timeout $($job.Name)"}
    if (-not (Test-Path "$out/$($job.Name)_1.png")) {throw "Missing screenshot $($job.Name)"}
    if (Select-String -Path "$out/$($job.Name).log" -Pattern 'Exception|Missing texture') {throw "Runtime error $($job.Name)"}
    Write-Output "Captured $($job.Name) with alternative hair"
  }
}
