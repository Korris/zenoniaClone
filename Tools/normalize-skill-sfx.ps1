# Normalizes one generated skill sound: trims leading silence (found with silencedetect, cut with -ss; silenceremove mangled these clips),
# optional low-pass, 40 ms fade-out, mono 44.1 kHz, peak-normalized to -PeakDb, mp3 160k.
# Usage: powershell -File Tools/normalize-skill-sfx.ps1 -In raw.opus -Out Assets/Resources/Audio/sfx/skills/wizard/fireball-cast.mp3 [-PeakDb -1] [-LowPassHz 0]
param(
    [Parameter(Mandatory = $true)][string]$In,
    [Parameter(Mandatory = $true)][string]$Out,
    [double]$PeakDb = -1,
    [int]$LowPassHz = 0
)
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture

function Invoke-FfmpegLog([string[]]$ffArgs) {
    # ffmpeg writes analysis results to stderr; collect them as plain text
    $psi = New-Object Diagnostics.ProcessStartInfo 'ffmpeg', ($ffArgs -join ' ')
    $psi.RedirectStandardError = $true; $psi.UseShellExecute = $false
    $p = [Diagnostics.Process]::Start($psi); $log = $p.StandardError.ReadToEnd(); $p.WaitForExit()
    if ($p.ExitCode -ne 0) { throw "ffmpeg failed: $log" }
    return $log
}

# Leading silence: only a silence that starts at 0 counts
$log = Invoke-FfmpegLog @('-hide_banner', '-i', "`"$In`"", '-af', 'silencedetect=noise=-40dB:d=0.02', '-f', 'null', '-')
$lead = 0.0
if ($log -match 'silence_start: (-?[0-9.e-]+)' -and [double]::Parse($Matches[1], $inv) -le 0.001 -and $log -match 'silence_end: ([0-9.]+)') {
    $lead = [Math]::Max(0.0, [double]::Parse($Matches[1], $inv) - 0.01) # keep 10 ms of attack
}

$dur = [double]::Parse((& ffprobe -v error -show_entries format=duration -of csv=p=0 "$In").Trim(), $inv)
$filters = @()
if ($LowPassHz -gt 0) { $filters += "lowpass=f=$LowPassHz" }
# 40 ms fade-out so a clip generated with a hard 1 s cut does not click
$filters += "afade=t=out:st=" + [Math]::Max(0.0, $dur - $lead - 0.04).ToString('0.000', $inv) + ":d=0.04"
$chain = ($filters -join ',') + ','
# explicit average downmix: aformat/-ac downmix level depends on filter-graph format negotiation (measured 3 dB apart)
$ch = [int]((& ffprobe -v error -select_streams a:0 -show_entries stream=channels -of csv=p=0 "$In").Trim())
$mono = if ($ch -ge 2) { "pan=mono|c0=0.5*c0+0.5*c1," } else { "" }
$ss = $lead.ToString('0.000', $inv)

# Peak after trim + filters, then the gain that puts it at -PeakDb
$log = Invoke-FfmpegLog @('-hide_banner', '-ss', $ss, '-i', "`"$In`"", '-af', "${chain}${mono}volumedetect", '-f', 'null', '-')
if ($log -notmatch 'max_volume: (-?[0-9.]+) dB') { throw "no volumedetect output for $In" }
$gain = $PeakDb - [double]::Parse($Matches[1], $inv)
$g = $gain.ToString('0.00', $inv)

New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
Invoke-FfmpegLog @('-hide_banner', '-y', '-ss', $ss, '-i', "`"$In`"", '-af', "${chain}${mono}volume=${g}dB",
    '-ar', '44100', '-ac', '1', '-c:a', 'libmp3lame', '-b:a', '160k', "`"$Out`"") | Out-Null
Write-Output ("{0} lead={1}s gain={2}dB" -f (Split-Path $Out -Leaf), $ss, $g)
