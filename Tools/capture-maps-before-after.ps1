<#
.SYNOPSIS
  One in-game screenshot per map from two player builds (before / after a map art change) and a side-by-side sheet per map.
  Each run: --map <id> [--tp x,y] --god --zoom Z --shots 4, 1280x720 window. Sheets go to -OutDir as <map>-ingame-before-after.jpg.
.EXAMPLE
  .\Tools\capture-maps-before-after.ps1 -Before D:\temp\player-portal\Zenonia.exe -After D:\temp\player-maps\Zenonia.exe -OutDir docs\reference\maps-ro-soft-fantasy\ingame
#>
param(
  [Parameter(Mandatory = $true)][string]$Before,
  [Parameter(Mandatory = $true)][string]$After,
  [Parameter(Mandatory = $true)][string]$OutDir,
  [string]$Work = "D:\temp\maps-before-after",
  [double]$Zoom = 110,
  [string[]]$Maps = @("frostHollow", "oakhavenVillage", "thatchVillage", "willowbrookMeadows", "sunbreakFoothills", "bramblewoodForest", "rootburrow",
    "thornheartChamber", "sunspireOasis", "amberDunes", "scorpionGorge", "emberfenMarsh", "sunkenReliquary", "frostfangDen")
)
$ErrorActionPreference = "Stop"
# Hero tile per map (empty = the map's own start). Picked to show ground + props, not a bare edge.
$spot = @{ sunkenReliquary = "24,15"; frostHollow = ""; willowbrookMeadows = "" }
New-Item -ItemType Directory -Force -Path $Work, $OutDir | Out-Null
foreach ($m in $Maps) {
  foreach ($side in @(@("before", $Before), @("after", $After))) {
    $shot = Join-Path $Work "$m-$($side[0])"
    Remove-Item "$shot*.png" -ErrorAction SilentlyContinue
    $a = @("--map", $m, "--class", "warrior", "--god", "--zoom", $Zoom, "--shots", "4", "--shot-path", $shot, "-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0", "-logFile", "$shot.log")
    if ($spot[$m]) { $a += @("--tp", $spot[$m]) }
    $p = Start-Process $side[1] -ArgumentList $a -PassThru
    if (-not $p.WaitForExit(60000)) { $p.Kill(); Write-Host "timeout $m $($side[0])" }
  }
  python -c "
import sys
from PIL import Image, ImageDraw
a=Image.open(r'$Work\$m-before_1.png').convert('RGB'); b=Image.open(r'$Work\$m-after_1.png').convert('RGB')
W=Image.new('RGB',(a.width+b.width+8,a.height+30),(20,20,28)); d=ImageDraw.Draw(W)
W.paste(a,(0,30)); W.paste(b,(a.width+8,30)); d.text((8,8),'TRUOC  $m',fill=(255,255,255)); d.text((a.width+16,8),'SAU (RO Soft Fantasy)  $m',fill=(255,255,255))
W.save(r'$OutDir\$m-ingame-before-after.jpg',quality=88)
"
  Write-Host "$m done"
}
