# Recreates the Unity test project D:\temp\proto3d for the 3D top-down prototype on a fresh machine, then (optionally) builds it.
# Needs: this repo (branch unity3d), the Tripo/Quaternius assets restored to D:\temp\3d-warrior (set8\, props\, monster\, audio\ — kept outside git,
# backed up on Drive), Unity 6000.3.9f1 (Hub default path). Usage, from the repo root:
#   powershell -ExecutionPolicy Bypass -File Prototypes\3d-topdown-warrior\setup-proto3d.ps1 [-Build] [-Assets D:\temp\3d-warrior] [-Project D:\temp\proto3d]
param([string]$Assets = "D:\temp\3d-warrior", [string]$Project = "D:\temp\proto3d", [switch]$Build)
$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$src = $PSScriptRoot
function Need($p) { if (-not (Test-Path $p)) { throw "Missing: $p" } }
Need "$Assets\set8"; Need "$Assets\props\sword"; Need "$Assets\monster\GreenBlob.fbx"

foreach ($d in "Assets\Scripts", "Assets\Editor", "Assets\Env", "Assets\Warrior", "Assets\Monster", "Assets\Props", "Assets\Audio", "Packages", "ProjectSettings") { New-Item -ItemType Directory -Force (Join-Path $Project $d) | Out-Null }
Copy-Item "$src\Scripts\*.cs" "$Project\Assets\Scripts\" -Force
Copy-Item "$src\Editor\*.cs" "$Project\Assets\Editor\" -Force
Copy-Item "$src\Data\*.txt" "$Project\Assets\Env\" -Force
Copy-Item "$src\project\Packages\manifest.json" "$Project\Packages\" -Force
Copy-Item "$src\project\ProjectSettings\*" "$Project\ProjectSettings\" -Force

# Warrior: the rigged Tripo FBX (8 clips) + its textures, renamed to what ProtoBuilder expects
$fbx = Get-ChildItem "$Assets\set8\*.fbx" | Select-Object -First 1
Copy-Item $fbx.FullName "$Project\Assets\Warrior\warrior.fbx" -Force
Copy-Item "$Assets\set8\*.fbm\*.png" "$Project\Assets\Warrior\" -Force
# Props: each Tripo folder -> Assets/Props/<name>/<name>.fbx + textures
foreach ($dir in Get-ChildItem "$Assets\props" -Directory) {
    $dst = "$Project\Assets\Props\$($dir.Name)"; New-Item -ItemType Directory -Force $dst | Out-Null
    $f = Get-ChildItem "$($dir.FullName)\*.fbx" | Select-Object -First 1
    Copy-Item $f.FullName "$dst\$($dir.Name).fbx" -Force
    Copy-Item "$($dir.FullName)\*.fbm\*.png" $dst -Force
}
Copy-Item "$Assets\monster\*" "$Project\Assets\Monster\" -Force
Copy-Item "$Assets\audio\*.mp3" "$Project\Assets\Audio\" -Force
# Ground paintings of the 2D maps (in the main repo)
Copy-Item "$repo\Assets\Resources\Sprites\tiles\frost-hollow-large-ground-r*.png" "$Project\Assets\Env\" -Force
Copy-Item "$repo\Assets\Resources\Sprites\village\tiles\willowbrook-large-ground-r*.png" "$Project\Assets\Env\" -Force
Write-Host "proto3d ready at $Project"

if ($Build) {
    $unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"; Need $unity
    & $unity -batchmode -nographics -projectPath $Project -executeMethod ProtoBuilder.Build -quit -logFile "D:\temp\proto3d-build.log"
    Select-String -Path "D:\temp\proto3d-build.log" -Pattern "\[proto\]|error CS" | ForEach-Object { $_.Line }
    Write-Host "exe: D:\temp\proto3d-build\Warrior3D.exe"
}
