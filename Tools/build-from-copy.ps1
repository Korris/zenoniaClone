<#
.SYNOPSIS
  Builds the Windows player from a COPY of the project, so a Unity Editor that has the real project open is never disturbed.
  Mirrors Assets\Scripts, Plugins, Editor and Resources (not the .meta files, so the copy keeps its own Library / import state) into -CopyDir
  and runs the PlayerBuildScript in batch mode.  First use: robocopy the whole project once (without Temp / Logs) into -CopyDir.
.EXAMPLE
  .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player\Zenonia.exe
#>
param(
  [Parameter(Mandatory = $true)][string]$CopyDir,
  [Parameter(Mandatory = $true)][string]$OutExe,
  [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe",
  [string]$Log = ""
)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path $CopyDir)) { throw "CopyDir not found: first copy the project there once (robocopy $repo $CopyDir /E /XD Temp Logs Build)" }
foreach ($d in "Scripts", "Plugins", "Editor", "Resources") {
  robocopy "$repo\Assets\$d" "$CopyDir\Assets\$d" /MIR /NFL /NDL /NJH /NJS /NP /XF "*.meta" | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $d (code $LASTEXITCODE)" }
}
if ($Log -eq "") { $Log = Join-Path (Split-Path $OutExe -Parent) "build.log" }
New-Item -ItemType Directory -Force -Path (Split-Path $OutExe -Parent) | Out-Null
& $Unity -batchmode -quit -projectPath $CopyDir -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildWindows -buildOut $OutExe -logFile $Log
$result = Select-String -Path $Log -Pattern "BUILD RESULT" | Select-Object -Last 1
Write-Host $result.Line
if ($result.Line -notmatch "Succeeded") { exit 1 }
