<#
.SYNOPSIS
  Builds the Windows player from a COPY of the project, so a Unity Editor that has the real project open is never disturbed.
  Mirrors Assets\Scripts, Plugins, Editor and Resources (not the .meta files, so the copy keeps its own Library / import state) into -CopyDir
  and runs the PlayerBuildScript in batch mode.  First use: robocopy the whole project once (without Temp / Logs) into -CopyDir.
  -Release makes a public build: Pocket Valor, PocketValor.exe in the folder of -OutExe, the API address baked from -ApiUrl (must be https), no dev-start.json, no F10, no dev page, no Server field.
  Without -Release it is the normal test build, cheats and all.
  -WebGL builds the browser version instead (web HUD, HTML chat input): -OutExe is then the output FOLDER (index.html + Build\).
  Needs the "Web Build Support" module for this Unity version (Unity Hub > Installs > Add modules); the build says so when it is missing.
.EXAMPLE
  .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player\Zenonia.exe
  .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\release\PocketValor.exe -Release -ApiUrl https://pocketvalor.luckyday.io.vn
  .\Tools\build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\web -WebGL
#>
param(
  [Parameter(Mandatory = $true)][string]$CopyDir,
  [Parameter(Mandatory = $true)][string]$OutExe,
  [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe",
  [string]$Log = "",
  [switch]$Release,
  [switch]$WebGL,
  [switch]$FarmTrial,
  [switch]$WizardNorthwind,
  [switch]$WizardNorthwindCombos,
  [switch]$WizardAstral,
  [switch]$WizardOriginal,
  [switch]$WizardGrandeur,
  [switch]$FrameHairstyles,
  [string]$ApiUrl = ""
)
$ErrorActionPreference = "Stop"
if (([int]$WizardNorthwind.IsPresent + [int]$WizardAstral.IsPresent + [int]$WizardOriginal.IsPresent + [int]$WizardGrandeur.IsPresent) -gt 1) { throw 'Choose one Wizard comparison variant' }
$repo = Split-Path $PSScriptRoot -Parent
if ($Release -and $ApiUrl -notmatch '^https://[^/\s]+') { Write-Host "BUILD RESULT: Failed. A release build needs -ApiUrl https://... (got '$ApiUrl')"; exit 1 }
if (-not $Release -and $ApiUrl -ne "") { Write-Host "BUILD RESULT: Failed. -ApiUrl only goes with -Release"; exit 1 }
if (-not (Test-Path $CopyDir)) { throw "CopyDir not found: first copy the project there once (robocopy $repo $CopyDir /E /XD Temp Logs Build)" }
foreach ($d in "Scripts", "Plugins", "Editor", "Resources") {
  robocopy "$repo\Assets\$d" "$CopyDir\Assets\$d" /MIR /NFL /NDL /NJH /NJS /NP /XF "*.meta" | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $d (code $LASTEXITCODE)" }
}
if ($FarmTrial) {
  # Trial-only settings in the COPY: preserve the user's real dev-start.json.
  $trialConfigPath = Join-Path $CopyDir 'Assets/Resources/Data/dev-start.json'
  $trialConfig = Get-Content -LiteralPath $trialConfigPath -Raw | ConvertFrom-Json
  $trialConfig.noCooldowns = $false
  $trialConfig.noMpCost = $true
  $trialConfig.level = 1
  $trialConfig.allSkills = $true
  $trialConfig | ConvertTo-Json | Set-Content -LiteralPath $trialConfigPath -Encoding UTF8
}
if ($WizardNorthwind) {
  # Comparison build only: prepare the copied Resources after syncing the live
  # project. The installer rejects targets outside Build and hashes live assets.
  & python (Join-Path $repo 'Tools/install-wizard-northwind-v7.py') install --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Northwind copy preparation failed' }
}
if ($WizardNorthwindCombos) {
  if (-not $WizardNorthwind) { throw 'WizardNorthwindCombos requires WizardNorthwind' }
  & python (Join-Path $repo 'Tools/install-northwind-default-attacks.py') --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Northwind default attack installation failed' }
  & python (Join-Path $repo 'Tools/install-northwind-t9-combos.py') --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Northwind combo installation failed' }
  & python (Join-Path $repo 'Tools/install-wizard-unified-model.py') --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Northwind unified model installation failed' }
}
if ($WizardGrandeur) {
  & python (Join-Path $repo 'Tools/install-wizard-current-reference.py') --target $CopyDir --body-source (Join-Path $repo 'Assets/ArtLibrary/classes/wizard/outfits/grandeur-20261109/src') --output (Join-Path $repo 'Build/wizard-grandeur')
  if ($LASTEXITCODE -ne 0) { throw 'Wizard grandeur copy preparation failed' }
}
if ($Log -eq "") { $Log = Join-Path (Split-Path $OutExe -Parent) "build.log" }
if ($WizardAstral) {
  & python (Join-Path $repo 'Tools/install-wizard-astral-v7.py') install --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Astral copy preparation failed' }
}
New-Item -ItemType Directory -Force -Path (Split-Path $OutExe -Parent) | Out-Null
if ($WizardOriginal) {
  & python (Join-Path $repo 'Tools/install-wizard-original.py') --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Original Wizard copy preparation failed' }
}
if ($FrameHairstyles) {
  & python (Join-Path $repo 'Tools/compile-avatar-hair-frames.py') --target $CopyDir
  if ($LASTEXITCODE -ne 0) { throw 'Complete-frame hairstyle compilation failed' }
}
Remove-Item $Log -ErrorAction SilentlyContinue # a line from the last build must not pass for this one
$method = if ($WebGL) { "BuildWebGL" } else { "BuildWindows" }
$unityArgs = @("-batchmode", "-quit", "-projectPath", $CopyDir, "-executeMethod", "ZenoniaProto.EditorTools.PlayerBuildScript.$method", "-buildOut", $OutExe, "-logFile", $Log)
if ($Release) { $unityArgs += @("-zenoniaRelease", "-apiUrl", $ApiUrl) }
& $Unity @unityArgs | Out-Null # the pipe makes PowerShell wait: Unity.exe is a GUI program
$result = Select-String -Path $Log -Pattern "BUILD RESULT" -ErrorAction SilentlyContinue | Select-Object -Last 1
if ($null -eq $result) { Write-Host "BUILD RESULT: Failed. Unity stopped before building (compile errors?), see $Log"; exit 1 }
Write-Host $result.Line
if ($result.Line -notmatch "Succeeded") { exit 1 }
