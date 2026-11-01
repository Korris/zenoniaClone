<#
.SYNOPSIS
  Records the hero select's preview clips from the game itself: Resources/Video/hero-select/<class>.mp4. Run again whenever sprites change; then build again to include them.
  Needs a built player with --record-compare and an ffmpeg (-Ffmpeg). The clips are 436 x 274, 30 fps, no sound.
.EXAMPLE
  .\Tools\record-hero-previews.ps1 -Exe C:\temp\player-kiem-goc\Zenonia.exe -Ffmpeg C:\path\ffmpeg.exe
#>
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [Parameter(Mandatory = $true)][string]$Ffmpeg,
  [string[]]$Classes = @("warrior", "wizard", "mechanic", "assassin", "cleric"),
  [string]$Work = "C:\temp\preview-frames"
)
$ErrorActionPreference = "Stop"
$out = Join-Path (Split-Path $PSScriptRoot -Parent) "Assets\Resources\Video\hero-select"
New-Item -ItemType Directory -Force $out | Out-Null
foreach ($c in $Classes) {
  $d = Join-Path $Work $c
  if (Test-Path $d) { Get-ChildItem $d -File | Remove-Item -Force }
  New-Item -ItemType Directory -Force $d | Out-Null
  $p = Start-Process $Exe -WorkingDirectory (Split-Path $Exe) -PassThru -ArgumentList "--class $c --record-compare preview --shot-path `"$d`" -screen-fullscreen 0 -screen-width 1138 -screen-height 640 -logFile `"$d\log.txt`""
  if (-not $p.WaitForExit(300000)) { Stop-Process $p -Force; throw "$c timed out" }
  Select-String "$d\log.txt" -Pattern "preview cast|recorded compare" | ForEach-Object { $_.Line }
  & $Ffmpeg -y -loglevel error -framerate 60 -i "$d\frame_%05d.png" -vf "crop=1018:640:0:0,scale=436:274:flags=lanczos,fps=30" -an -c:v libx264 -profile:v baseline -pix_fmt yuv420p -crf 21 -bf 0 -movflags +faststart "$out\$c.mp4"
  "{0}: {1} KB" -f $c, [int]((Get-Item "$out\$c.mp4").Length / 1KB)
}
