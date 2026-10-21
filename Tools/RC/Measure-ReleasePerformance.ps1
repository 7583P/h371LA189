param(
    [string]$Player = 'Builds/ReleaseGates-20261019-01-Validated/HillAdventurer-HumanGate.exe',
    [string]$EvidenceRoot = 'Docs/ReleaseGates/20261019-01'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$playerPath = (Resolve-Path -LiteralPath (Join-Path $taskRoot $Player)).Path
$folder = Split-Path -Parent $playerPath
$name = [IO.Path]::GetFileNameWithoutExtension($playerPath)
$identity = @(Get-Content -LiteralPath (Join-Path $folder ($name + '_Data/app.info')))
if ($identity[0].Trim() -ne 'HillAdventurer.BlacklineQA') { throw 'Only the isolated QA player is authorized' }
if (Get-Process -Name 'Unity','HillAdventurer-HumanGate' -ErrorAction SilentlyContinue) { throw 'Another Unity/player is open; no valid measurement can start' }
$model = Join-Path $taskRoot 'Tools/ML/Models/Candidates/RC20261006/behavior_mlp.bytes'
$artifactPaths = @($playerPath,(Join-Path $folder 'UnityPlayer.dll'),(Join-Path $folder ($name + '_Data/Managed/Assembly-CSharp.dll')))
function Read-Hashes {
    $hashes = [ordered]@{}
    foreach ($artifact in $artifactPaths) { $hashes[$artifact] = (Get-FileHash -LiteralPath $artifact).Hash }
    return $hashes
}
$run = Join-Path (Join-Path $taskRoot $EvidenceRoot) ('focused-performance-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
$null = New-Item -ItemType Directory -Path $run
$snapshot = Join-Path $taskRoot ('tmp/ReleaseGates/performance-playerprefs-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $snapshot
$qaRegistry = 'HKCU\Software\' + $identity[0].Trim() + '\' + $identity[1].Trim()
if (Test-Path -LiteralPath ('Registry::HKEY_CURRENT_USER\Software\' + $identity[0].Trim() + '\' + $identity[1].Trim())) {
    & reg.exe export $qaRegistry (Join-Path $snapshot 'before.reg')
    if ($LASTEXITCODE -ne 0) { throw 'QA preferences backup failed' }
}
foreach ($mode in @('Off','Shadow')) {
    if (Get-Process -Name 'Unity','HillAdventurer-HumanGate' -ErrorAction SilentlyContinue) { throw 'Concurrent Unity/player appeared; measurement blocked' }
    $directory = Join-Path $run ('performance-' + $mode.ToLowerInvariant() + '-policy')
    $null = New-Item -ItemType Directory -Path $directory
    $arguments = @('-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-force-d3d11',
        '--closure-player','--closure-mode','profile','--closure-seeds','4207','--closure-mmr','10000','--closure-timescale','1',
        '--racing-ml-mode',$mode,'--racing-shadow-weights',$model,'--rainbow-preview',
        '--frame-forensics','--forensics-variant','reduced','--forensics-render','native','--forensics-seconds','60',
        '--blackline-onscreen','--buffered-driving-traces','--closure-output',$directory,'-logFile',($directory + '.log'))
    $manifest = [ordered]@{classification='FOCUSED_PARTIAL_60S_HOST_PAIR_NOT_A_FULL_RACE_OR_ANDROID_GATE';mode=$mode;arguments=$arguments;
        startedUtc=[DateTime]::UtcNow.ToString('o');artifactsBefore=Read-Hashes;modelSha256=(Get-FileHash -LiteralPath $model).Hash;
        powerPlanBefore=((& powercfg /getactivescheme) -join "`n").Trim();qaPreferenceSnapshot=$snapshot}
    $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $directory 'measurement-started.json') -Encoding UTF8
    Write-Output "Keep the $mode game window in front, on AC power for the entire 60-second measured window."
    $argumentLine = ($arguments | ForEach-Object { '"' + $_.Replace('"','\"') + '"' }) -join ' '
    # Intentionally visible: this is a manual foreground measurement, never an unattended helper.
    $process = Start-Process -FilePath $playerPath -ArgumentList $argumentLine -WorkingDirectory $taskRoot -WindowStyle Normal -Wait -PassThru
    $manifest.exitCode=$process.ExitCode;$manifest.finishedUtc=[DateTime]::UtcNow.ToString('o');$manifest.artifactsAfter=Read-Hashes
    $manifest.powerPlanAfter=((& powercfg /getactivescheme) -join "`n").Trim()
    $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $directory 'measurement-run.json') -Encoding UTF8
    & python (Join-Path $PSScriptRoot 'analyze_performance.py') (Join-Path $directory 'seed-4207')
    if ($LASTEXITCODE -ne 0) { throw 'Incomplete sample retained; cannot compare' }
}
& python (Join-Path $PSScriptRoot 'compare_performance.py') $run
if ($LASTEXITCODE -ne 0) { throw 'Comparison failed' }
$comparison = Get-Content -LiteralPath (Join-Path $run 'performance-comparison.json') -Raw | ConvertFrom-Json
if (-not $comparison.accepted) { throw ('Pair rejected: ' + ($comparison.rejection_reasons -join '; ')) }
Write-Output "Accepted local pair retained at $run. Production ML remains Off."
