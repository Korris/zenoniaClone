$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $repo 'Build/Avatar-Hair-Frames-WizardFix/Zenonia.exe'
$out=Join-Path $repo 'Build/Avatar-Hair-Frames-WizardFix/captures'
New-Item -ItemType Directory -Force $out | Out-Null
$cases=@(
  @{Name='idle-left';Face='left';Style='0';Bot='';Tier='0'},
  @{Name='idle-right';Face='right';Style='0';Bot='';Tier='0'},
  @{Name='attack-left';Face='left';Style='0';Bot='attack';Tier='0'},
  @{Name='attack-right';Face='right';Style='0';Bot='attack';Tier='0'},
  @{Name='attack-down';Face='down';Style='0';Bot='attack';Tier='0'},
  @{Name='walk-right';Face='right';Style='0';Bot='walk';Tier='0'},
  @{Name='hair1-right';Face='right';Style='1';Bot='attack';Tier='0'},
  @{Name='t9-right';Face='right';Style='0';Bot='attack';Tier='9'}
)
for ($i=0; $i -lt $cases.Count; $i+=2) {
  $jobs=@()
  foreach ($case in $cases[$i..($i+1)]) {
    $captureArgs=@('--class','wizard','--level','60','--worn-tier',$case.Tier,'--wear-tiers',"$($case.Tier),0,0",'--face',$case.Face,'--hairstyle',$case.Style,'--god','--offscreen-capture','--zoom','42','--tp','40,30','--shots','1.0,1.12,1.28,1.5,1.75','--shot-path',"$out/$($case.Name)",'-screen-width','900','-screen-height','900','-screen-fullscreen','0','-logFile',"$out/$($case.Name).log")
    if ($case.Bot) {$captureArgs+=@('--bot',$case.Bot)}
    $jobs+=@{Name=$case.Name;Process=(Start-Process $exe -ArgumentList $captureArgs -WindowStyle Hidden -PassThru)}
  }
  foreach ($job in $jobs) {
    if (-not $job.Process.WaitForExit(45000)) {throw "Timeout $($job.Name)"}
    if (-not (Test-Path "$out/$($job.Name)_1.png")) {throw "Missing screenshot $($job.Name)"}
    if (Select-String -Path "$out/$($job.Name).log" -Pattern 'Exception|Missing texture') {throw "Runtime error $($job.Name)"}
    Write-Output "Captured $($job.Name)"
  }
}
