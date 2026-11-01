<#
.SYNOPSIS
  Builds the Linux zone server (Dedicated Server subtarget, x86_64) from a COPY of the project, like build-from-copy.ps1. Run it on the result with --mode server --api-url URL and ZENONIA_SERVICE_KEY in the environment.
  Stops before anything is built, with the names of the Unity Hub modules, when they are not installed.
.EXAMPLE
  .\Tools\build-linux-server.ps1 -CopyDir C:\temp\zu-copy -OutFile C:\temp\linux-server\ZenoniaServer.x86_64
#>
param(
  [Parameter(Mandatory = $true)][string]$CopyDir,
  [Parameter(Mandatory = $true)][string]$OutFile,
  [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe",
  [string]$Log = "",
  [switch]$SkipModuleCheck # only to test the same check inside PlayerBuildScript
)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$engine = Join-Path (Split-Path $Unity -Parent) "Data\PlaybackEngines\LinuxStandaloneSupport"
$haveServer = (Test-Path "$engine\Variations") -and @(Get-ChildItem "$engine\Variations" -Directory -Filter "*server*" -ErrorAction SilentlyContinue).Count -gt 0
if (-not $SkipModuleCheck -and -not $haveServer) {
  Write-Host "BUILD RESULT: Failed. The Linux server modules are not installed for this Unity ($Unity)."
  Write-Host "In Unity Hub: Installs > 6000.3.9f1 > Add modules > 'Linux Build Support (Mono)' and 'Linux Dedicated Server Build Support'."
  exit 1
}
if (-not (Test-Path $CopyDir)) { throw "CopyDir not found: first copy the project there once (robocopy $repo $CopyDir /E /XD Temp Logs Build)" }
foreach ($d in "Scripts", "Plugins", "Editor", "Resources") {
  robocopy "$repo\Assets\$d" "$CopyDir\Assets\$d" /MIR /NFL /NDL /NJH /NJS /NP /XF "*.meta" | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $d (code $LASTEXITCODE)" }
}
if ($Log -eq "") { $Log = Join-Path (Split-Path $OutFile -Parent) "build.log" }
New-Item -ItemType Directory -Force -Path (Split-Path $OutFile -Parent) | Out-Null
Remove-Item $Log -ErrorAction SilentlyContinue
& $Unity -batchmode -quit -projectPath $CopyDir -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildLinuxServer -buildOut $OutFile -logFile $Log | Out-Null # the pipe makes PowerShell wait: Unity.exe is a GUI program
$result = Select-String -Path $Log -Pattern "BUILD RESULT" -ErrorAction SilentlyContinue | Select-Object -Last 1
if ($null -eq $result) { Write-Host "BUILD RESULT: Failed. Unity stopped before building (compile errors?), see $Log"; exit 1 }
Write-Host $result.Line
if ($result.Line -notmatch "Succeeded") { exit 1 }
