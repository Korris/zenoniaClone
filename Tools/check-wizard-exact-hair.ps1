param(
  [string]$Exe = "$PSScriptRoot/../Build/Wizard-Current-Reference/Zenonia.exe",
  [string]$Output = "$PSScriptRoot/../Build/wizard-current-reference/hair-captures"
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Exe)) { throw "Missing player: $Exe" }
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$cases = @(1..5 | ForEach-Object { @{ Name="hair$_"; Style=$_; Face='down'; Attack=$false } })
$cases += @(
  @{ Name='hair5-right'; Style=5; Face='right'; Attack=$false },
  @{ Name='hair5-up'; Style=5; Face='up'; Attack=$false },
  @{ Name='hair5-attack'; Style=5; Face='down'; Attack=$true }
)
foreach ($case in $cases) {
  $captureArgs = @('-screen-width','900','-screen-height','900','-screen-fullscreen','0',
    '--offscreen-capture','--god','--class','wizard','--level','60',
    '--worn-tier','0','--wear-tiers','0,0,0','--hairstyle',"$($case.Style)",
    '--face',$case.Face,'--tp','40,30','--zoom','42','--shots','1',
    '--shot-path',"$Output/$($case.Name)",'-logFile',"$Output/$($case.Name).log")
  if ($case.Attack) { $captureArgs += @('--bot','attack') }
  $captureProcess = Start-Process -FilePath $Exe -ArgumentList $captureArgs -WindowStyle Hidden -PassThru
  if (-not $captureProcess.WaitForExit(45000)) { throw "Capture timed out: $($case.Name)" }
  if (-not (Test-Path -LiteralPath "$Output/$($case.Name)_1.png")) { throw "Missing capture: $($case.Name)" }
  Write-Output "Captured $($case.Name)"
}
