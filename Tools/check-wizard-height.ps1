param(
  [string]$Exe = 'D:/zenoniaClone/Build/wizard-reference/player-height-fixed/Zenonia.exe',
  [string]$Output = 'D:/zenoniaClone/Build/wizard-reference/captures-height-fixed',
  [switch]$IdleOnly,
  [int]$Zoom = 30
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Exe)) { throw "Missing player: $Exe" }
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$cases = @(0..9 | ForEach-Object { @{ Name="t$_"; Tier=$_; Face='down'; Walk=$false } })
if (-not $IdleOnly) { $cases += @(
  @{ Name='right-idle'; Tier=9; Face='right'; Walk=$false },
  @{ Name='right-walk'; Tier=9; Face='right'; Walk=$true },
  @{ Name='up-idle'; Tier=9; Face='up'; Walk=$false }
) }
foreach ($case in $cases) {
  $captureArgs = @('-screen-width','900','-screen-height','900','-screen-fullscreen','0',
    '--offscreen-capture','--god','--class','wizard','--level','100',
    '--worn-tier',"$($case.Tier)",'--wear-tiers',"$($case.Tier),0,0",'--face',$case.Face,
    '--tp','40,30','--zoom',"$Zoom",'--shots','1','--shot-path',"$Output/$($case.Name)",
    '-logFile',"$Output/$($case.Name).log")
  if ($case.Walk) { $captureArgs += @('--bot','walk') }
  $captureProcess = Start-Process -FilePath $Exe -ArgumentList $captureArgs -WindowStyle Hidden -PassThru
  if (-not $captureProcess.WaitForExit(45000)) { throw "Capture timed out: $($case.Name)" }
  if (-not (Test-Path -LiteralPath "$Output/$($case.Name)_1.png")) { throw "Missing capture: $($case.Name)" }
  Write-Output "Captured $($case.Name)"
}
