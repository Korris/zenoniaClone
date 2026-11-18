# Recreates the Unity test project D:\temp\proto3d for the 3D top-down prototype on a fresh machine, then (optionally) builds it.
# Needs: this repo (branch unity3d), the Tripo/Quaternius assets restored to D:\temp\3d-warrior (set8\, props\, monster\, audio\ — kept outside git,
# backed up on Drive), Unity 6000.3.9f1 (Hub default path). Usage, from the repo root:
#   powershell -ExecutionPolicy Bypass -File Prototypes\3d-topdown-warrior\setup-proto3d.ps1 [-Build] [-Assets D:\temp\3d-warrior] [-Project D:\temp\proto3d]
param([string]$Assets = "D:\temp\3d-warrior", [string]$Project = "D:\temp\proto3d", [switch]$Build)
$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$src = $PSScriptRoot
function Need($p) { if (-not (Test-Path $p)) { throw "Missing: $p" } }
Need "$Assets\set8"; Need "$Assets\props\sword"; Need "$Assets\monster\leafdrop"

foreach ($d in "Assets\Scripts", "Assets\Editor", "Assets\Shaders", "Assets\Env", "Assets\Warrior", "Assets\Monster", "Assets\Props", "Assets\Audio", "Assets\Resources\ProtoHud", "Packages", "ProjectSettings") { New-Item -ItemType Directory -Force (Join-Path $Project $d) | Out-Null }
Copy-Item "$src\Scripts\*.cs" "$Project\Assets\Scripts\" -Force
Copy-Item "$src\Editor\*.cs" "$Project\Assets\Editor\" -Force
Copy-Item "$src\Shaders\*" "$Project\Assets\Shaders\" -Force
Copy-Item "$src\Data\*.txt" "$Project\Assets\Env\" -Force
Copy-Item "$src\project\Packages\manifest.json" "$Project\Packages\" -Force
Copy-Item "$src\project\ProjectSettings\*" "$Project\ProjectSettings\" -Force

# Warrior: the rigged Tripo FBX (8 clips) + its textures, renamed to what ProtoBuilder expects
$fbx = Get-ChildItem "$Assets\set8\*.fbx" | Select-Object -First 1
Copy-Item $fbx.FullName "$Project\Assets\Warrior\warrior.fbx" -Force
Copy-Item "$Assets\set8\*.fbm\*.png" "$Project\Assets\Warrior\" -Force
# Warrior V2 (character plan v1, 18/11): ChatGPT 4-view A-pose -> Tripo multiview, quad retopo 5.6k, Mixamo rig, 7 preset clips. Optional.
$v2 = Get-ChildItem "$Assets\warrior-v2\*.fbx" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($v2) {
    New-Item -ItemType Directory -Force "$Project\Assets\WarriorV2" | Out-Null
    Copy-Item $v2.FullName "$Project\Assets\WarriorV2\warrior-v2.fbx" -Force
    foreach ($t in Get-ChildItem "$Assets\warrior-v2\*.fbm\*.png") {
        $n = if ($t.Name -match 'BaseColor') { 'basecolor' } elseif ($t.Name -match 'Normal') { 'normal' } elseif ($t.Name -match 'metallic') { 'metallic' } else { 'roughness' }
        Copy-Item $t.FullName "$Project\Assets\WarriorV2\$n.png" -Force
    }
}
# Props: each Tripo folder -> Assets/Props/<name>/<name>.fbx + textures
foreach ($dir in Get-ChildItem "$Assets\props" -Directory) {
    $dst = "$Project\Assets\Props\$($dir.Name)"; New-Item -ItemType Directory -Force $dst | Out-Null
    $f = Get-ChildItem "$($dir.FullName)\*.fbx" | Select-Object -First 1
    Copy-Item $f.FullName "$dst\$($dir.Name).fbx" -Force
    Copy-Item "$($dir.FullName)\*.fbm\*.png" $dst -Force
}
# Leafdrop Slime (Tripo export folder) -> Assets/Monster/leafdrop/leafdrop.fbx + leafdrop_<map>.jpg
$lf = "$Project\Assets\Monster\leafdrop"; New-Item -ItemType Directory -Force $lf | Out-Null
Copy-Item (Get-ChildItem "$Assets\monster\leafdrop\*.fbx" | Select-Object -First 1).FullName "$lf\leafdrop.fbx" -Force
foreach ($t in Get-ChildItem "$Assets\monster\leafdrop\*.fbm\*.jpg") { Copy-Item $t.FullName ("$lf\leafdrop_" + ($t.BaseName -replace '^.*_', '') + ".jpg") -Force }
if (Test-Path "$Assets\audio") { Copy-Item "$Assets\audio\*.mp3" "$Project\Assets\Audio\" -Force }
else {   # no audio\ backup: take the warrior sounds straight from the main game
    $sfx = "$repo\Assets\Resources\Audio\sfx\warrior"
    Copy-Item "$sfx\slash1.mp3" "$Project\Assets\Audio\swing.mp3" -Force
    Copy-Item "$sfx\hit-a.mp3" "$Project\Assets\Audio\hit.mp3" -Force
    Copy-Item "$sfx\crit-a.mp3" "$Project\Assets\Audio\crit.mp3" -Force
}
# Ground paintings of the 2D maps (in the main repo)
Copy-Item "$repo\Assets\Resources\Sprites\tiles\frost-hollow-large-ground-r*.png" "$Project\Assets\Env\" -Force
Copy-Item "$repo\Assets\Resources\Sprites\village\tiles\willowbrook-large-ground-r*.png" "$Project\Assets\Env\" -Force
# HUD V2 + skill icons: ChatGPT pieces cut by docs\reference\3d-warrior-prototype\hud-3d-chatgpt\cut-hud3d-parts.py (prototype-only art)
Copy-Item "$repo\docs\reference\3d-warrior-prototype\hud-3d-chatgpt\parts\*.png" "$Project\Assets\Resources\ProtoHud\" -Force
# Glade (real 3D forest): each Tripo export folder $Assets\glade\<name>\ -> Assets\Glade\<name>\<name>.fbx + <name>_<map>.jpg (missing ones are skipped)
if (Test-Path "$Assets\glade") {
    foreach ($g in Get-ChildItem "$Assets\glade" -Directory) {
        $gd = "$Project\Assets\Glade\$($g.Name)"; New-Item -ItemType Directory -Force $gd | Out-Null
        $gf = Get-ChildItem "$($g.FullName)\*.fbx" | Select-Object -First 1
        if ($gf) { Copy-Item $gf.FullName "$gd\$($g.Name).fbx" -Force }
        foreach ($t in Get-ChildItem "$($g.FullName)\*.fbm\*.jpg") { Copy-Item $t.FullName ("$gd\$($g.Name)_" + ($t.BaseName -replace '^.*_', '') + ".jpg") -Force }
    }
}
# Glade ground textures: ChatGPT 2x2 tileable sheet split into glade-grass / grass-light / dirt / soil (forest-3d-chatgpt)
Copy-Item "$repo\docs\reference\3d-warrior-prototype\forest-3d-chatgpt\glade-*.png" "$Project\Assets\Env\" -Force
# v9 leaf atlas: ChatGPT leaf-cluster sheet keyed to alpha (docs\reference\3d-warrior-prototype\foliage-3d-chatgpt)
Copy-Item "$repo\docs\reference\3d-warrior-prototype\foliage-3d-chatgpt\leaf-atlas-4x4.png" "$Project\Assets\Env\" -Force
Write-Host "proto3d ready at $Project"

if ($Build) {
    $unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"; Need $unity
    $out = Split-Path $Project -Parent; $log = Join-Path $out "proto3d-build.log"   # ProtoBuilder writes the player to <project>\..\proto3d-build
    & $unity -batchmode -nographics -projectPath $Project -executeMethod ProtoBuilder.Build -quit -logFile $log | Out-Null
    Select-String -Path $log -Pattern "\[proto\]|error CS" | ForEach-Object { $_.Line }
    Write-Host "exe: $out\proto3d-build\Warrior3D.exe"
}
