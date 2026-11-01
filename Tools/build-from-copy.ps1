<#
.SYNOPSIS
  Builds the Windows player from a COPY of the project, so a Unity Editor that has the real project open is never disturbed.
  Mirrors Assets\Scripts, Plugins, Editor and Resources (not the .meta files, so the copy keeps its own Library / import state) into -CopyDir
  and runs the PlayerBuildScript in batch mode.  First use: robocopy the whole project once (without Temp / Logs) into -CopyDir.
  -Release makes a public build: Pocket Valor, PocketValor.exe in the folder of -OutExe, the API address baked from -ApiUrl (must be https), no dev-start.json, no F10, no dev page, no Server field.
  Without -Release it is the normal test build, cheats and all.
.EXAMPLE
  .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player\Zenonia.exe
  .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\release\PocketValor.exe -Release -ApiUrl https://pocketvalor.luckyday.io.vn
#>
param(
  [Parameter(Mandatory = $true)][string]$CopyDir,
  [Parameter(Mandatory = $true)][string]$OutExe,
  [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe",
  [string]$Log = "",
  [switch]$Release,
  [string]$ApiUrl = ""
)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
if ($Release -and $ApiUrl -notmatch '^https://[^/\s]+') { Write-Host "BUILD RESULT: Failed. A release build needs -ApiUrl https://... (got '$ApiUrl')"; exit 1 }
if (-not $Release -and $ApiUrl -ne "") { Write-Host "BUILD RESULT: Failed. -ApiUrl only goes with -Release"; exit 1 }
if (-not (Test-Path $CopyDir)) { throw "CopyDir not found: first copy the project there once (robocopy $repo $CopyDir /E /XD Temp Logs Build)" }
foreach ($d in "Scripts", "Plugins", "Editor", "Resources") {
  robocopy "$repo\Assets\$d" "$CopyDir\Assets\$d" /MIR /NFL /NDL /NJH /NJS /NP /XF "*.meta" | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $d (code $LASTEXITCODE)" }
}
if ($Log -eq "") { $Log = Join-Path (Split-Path $OutExe -Parent) "build.log" }
New-Item -ItemType Directory -Force -Path (Split-Path $OutExe -Parent) | Out-Null
Remove-Item $Log -ErrorAction SilentlyContinue # a line from the last build must not pass for this one
$unityArgs = @("-batchmode", "-quit", "-projectPath", $CopyDir, "-executeMethod", "ZenoniaProto.EditorTools.PlayerBuildScript.BuildWindows", "-buildOut", $OutExe, "-logFile", $Log)
if ($Release) { $unityArgs += @("-zenoniaRelease", "-apiUrl", $ApiUrl) }
& $Unity @unityArgs | Out-Null # the pipe makes PowerShell wait: Unity.exe is a GUI program
$result = Select-String -Path $Log -Pattern "BUILD RESULT" -ErrorAction SilentlyContinue | Select-Object -Last 1
if ($null -eq $result) { Write-Host "BUILD RESULT: Failed. Unity stopped before building (compile errors?), see $Log"; exit 1 }
Write-Host $result.Line
if ($result.Line -notmatch "Succeeded") { exit 1 }
