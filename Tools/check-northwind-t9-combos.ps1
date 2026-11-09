$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $root 'Build/Northwind-T9-Combo/Zenonia.exe'
$out = Join-Path $root 'Build/Northwind-T9-Combo/captures'
New-Item -ItemType Directory -Force $out | Out-Null
foreach ($case in @(@{Name='left';Dir='left';Tier=9;Hair=5},@{Name='right';Dir='right';Tier=9;Hair=5},@{Name='default-down';Dir='down';Tier=0;Hair=0},@{Name='default-down-hair5';Dir='down';Tier=0;Hair=5},@{Name='default-left';Dir='left';Tier=0;Hair=0},@{Name='default-right';Dir='right';Tier=0;Hair=0},@{Name='default-up';Dir='up';Tier=0;Hair=0},@{Name='default-idle-down';Dir='down';Tier=0;Hair=0;Idle=$true})) {
  $dir=$case.Name
  $captureArguments = @('--class','wizard','--level','60','--worn-tier',"$($case.Tier)",'--wear-tiers',"$($case.Tier),0,0",'--face',$case.Dir,'--hairstyle',"$($case.Hair)",'--offscreen-capture','--god','--tp','40,30','--zoom','42','--shots','1,1.12,1.28,1.5,1.75','--shot-path',"$out/$dir",'-screen-width','900','-screen-height','900','-screen-fullscreen','0','-logFile',"$out/$dir.log")
  if (-not $case.Idle) { $captureArguments += @('--bot','attack') }
  $process = Start-Process $exe -ArgumentList $captureArguments -WindowStyle Hidden -PassThru
  if (-not $process.WaitForExit(45000)) { throw "Timed out: $dir" }
  if (-not (Test-Path "$out/${dir}_5.png")) { throw "Missing capture: $dir" }
}
