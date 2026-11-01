<#
.SYNOPSIS
  Makes one archive to copy to the Ubuntu server: the API, the Linux zone server, the pm2 config and the settings from deploy\.env.prod.
  The archive holds real secrets: copy it by scp only, never commit or share it. On the server: unpack, then bash start.sh (docs/deploy.md).
.EXAMPLE
  .\Tools\package-server.ps1
.EXAMPLE
  .\Tools\package-server.ps1 -ApiOnly
#>
param(
  [string]$Out = "C:\temp\pocketvalor-server.tar.gz",
  [string]$EnvFile = "",
  [string]$CopyDir = "C:\temp\zu-copy-linux",
  [string]$BuildDir = "C:\temp\linux-server",
  [switch]$SkipBuild, # reuse the zone server already in BuildDir
  [switch]$ApiOnly    # no zone server, e.g. when only the API changed
)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
if ($EnvFile -eq "") { $EnvFile = Join-Path $repo "deploy\.env.prod" }
if (-not (Test-Path $EnvFile)) { throw "No settings file: $EnvFile" }

$settings = @{}
foreach ($line in Get-Content $EnvFile) { if ($line -match '^\s*([A-Z_]+)=(.*)$') { $settings[$Matches[1]] = $Matches[2].Trim() } }
foreach ($need in "DOMAIN", "JWT_SECRET", "SERVICE_KEY", "MONGO_URI") {
  if ([string]::IsNullOrEmpty($settings[$need])) { throw "$need is empty in $EnvFile" }
}
foreach ($warn in "SMTP_USER", "SMTP_PASS", "SMTP_FROM") {
  if ([string]::IsNullOrEmpty($settings[$warn])) { Write-Warning "$warn is empty: password reset mail will not be sent" }
}

$exe = Join-Path $BuildDir "ZenoniaServer.x86_64"
if (-not $ApiOnly -and -not $SkipBuild) {
  if (-not (Test-Path $CopyDir)) {
    robocopy $repo $CopyDir /E /XD Temp Logs Build Library .git node_modules /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed (code $LASTEXITCODE)" }
  }
  & (Join-Path $PSScriptRoot "build-linux-server.ps1") -CopyDir $CopyDir -OutFile $exe
  if ($LASTEXITCODE -ne 0) { exit 1 }
}
if (-not $ApiOnly -and -not (Test-Path $exe)) { throw "No zone server at $exe" }

$stage = Join-Path ([IO.Path]::GetTempPath()) ("pocketvalor-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path "$stage\api" | Out-Null
try {
  Copy-Item "$repo\server-api\src" "$stage\api\src" -Recurse
  Copy-Item "$repo\server-api\package.json", "$repo\server-api\package-lock.json" "$stage\api"
  Copy-Item "$repo\deploy\pm2\ecosystem.config.cjs", "$repo\deploy\pm2\start.sh", "$repo\deploy\zone\entrypoint.sh" $stage
  Copy-Item $EnvFile "$stage\.env"
  if (-not $ApiOnly) {
    robocopy $BuildDir "$stage\zone" /E /XF build.log /XD "*_BackUpThisFolder_ButDontShipItWithYourGame" "*_BurstDebugInformation_DoNotShip" /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed (code $LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
  }
  # Bash and .env on Linux must not see Windows line ends.
  foreach ($f in ".env", "start.sh", "entrypoint.sh", "ecosystem.config.cjs") {
    $text = [IO.File]::ReadAllText("$stage\$f")
    [IO.File]::WriteAllText("$stage\$f", $text.Replace("`r`n", "`n"), (New-Object Text.UTF8Encoding($false)))
  }
  New-Item -ItemType Directory -Force -Path (Split-Path $Out -Parent) | Out-Null
  & "$env:SystemRoot\System32\tar.exe" -czf $Out -C $stage .
  if ($LASTEXITCODE -ne 0) { throw "tar failed" }
} finally {
  Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
$mb = [math]::Round((Get-Item $Out).Length / 1MB, 1)
Write-Host "Done: $Out ($mb MB). It holds real secrets."
Write-Host "scp `"$Out`" user@SERVER:/tmp/"
Write-Host "On the server: mkdir -p /opt/pocketvalor && cd /opt/pocketvalor && rm -rf api zone && tar xzf /tmp/pocketvalor-server.tar.gz && rm /tmp/pocketvalor-server.tar.gz && bash start.sh"
