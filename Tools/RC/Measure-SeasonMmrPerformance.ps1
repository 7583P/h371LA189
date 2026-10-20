param(
    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$RunLabel = 'policy',
    [ValidateSet('Off', 'Shadow')]
    [string[]]$Modes = @('Off', 'Shadow')
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$player = Join-Path $taskRoot 'Builds/HumanGateMmrValidated/HillAdventurer-HumanGate.exe'
$model = Join-Path $taskRoot 'Tools/ML/Models/Candidates/RC20261006/behavior_mlp.bytes'
$evidence = Join-Path $taskRoot 'Logs/SeasonExpansion/20261018'
$artifacts = @(
    $player,
    (Join-Path $taskRoot 'Builds/HumanGateMmrValidated/UnityPlayer.dll'),
    (Join-Path $taskRoot 'Builds/HumanGateMmrValidated/HillAdventurer-HumanGate_Data/Managed/Assembly-CSharp.dll')
)
foreach ($path in @($artifacts) + @($model)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing measurement input: $path" }
}
if (Get-Process -Name 'Unity', 'HillAdventurer-HumanGate' -ErrorAction SilentlyContinue) {
    throw 'Wait for the existing Unity/player workload before measuring.'
}
foreach ($mode in $Modes) {
    $directory = Join-Path $evidence ('performance-' + $mode.ToLowerInvariant() + '-' + $RunLabel)
    if (Test-Path -LiteralPath $directory) { throw "Preserve previous evidence: $directory already exists." }
}
function Read-MeasurementHashes {
    $hashes = [ordered]@{}
    foreach ($path in $artifacts) {
        $relative = $path.Substring($taskRoot.Length + 1).Replace('\', '/')
        $hashes[$relative] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    }
    return $hashes
}
foreach ($mode in $Modes) {
    $directory = Join-Path $evidence ('performance-' + $mode.ToLowerInvariant() + '-' + $RunLabel)
    New-Item -ItemType Directory -Path $directory | Out-Null
    $arguments = @(
        '-screen-width', '1600', '-screen-height', '900', '-screen-fullscreen', '0', '-force-d3d11',
        '--closure-player', '--closure-mode', 'profile', '--closure-seeds', '4207', '--closure-mmr', '10000',
        '--closure-timescale', '1', '--racing-ml-mode', $mode, '--racing-shadow-weights', $model,
        '--frame-forensics', '--forensics-variant', 'reduced', '--forensics-render', 'native',
        '--forensics-seconds', '60', '--blackline-onscreen', '--buffered-driving-traces',
        '--closure-output', $directory, '-logFile', ($directory + '.log')
    )
    $manifest = [ordered]@{
        classification = 'LOCAL_FOREGROUND_PERFORMANCE_PARTIAL_60S_NOT_FULL_RACE_GATE'
        mode = $mode
        arguments = $arguments
        startedUtc = [DateTime]::UtcNow.ToString('o')
        artifactsBefore = Read-MeasurementHashes
        modelSha256 = (Get-FileHash -LiteralPath $model -Algorithm SHA256).Hash
        powerPlanBefore = ((& powercfg /getactivescheme) -join "`n").Trim()
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $directory 'measurement-started.json') -Encoding UTF8
    Write-Output "Starting native foreground $mode (60 measured seconds)."
    $argumentLine = ($arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
    $process = Start-Process -FilePath $player -ArgumentList $argumentLine -WorkingDirectory $taskRoot -WindowStyle Normal -Wait -PassThru
    $manifest.exitCode = $process.ExitCode
    $manifest.finishedUtc = [DateTime]::UtcNow.ToString('o')
    $manifest.artifactsAfter = Read-MeasurementHashes
    $manifest.powerPlanAfter = ((& powercfg /getactivescheme) -join "`n").Trim()
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $directory 'measurement-run.json') -Encoding UTF8
    $seedDirectory = Join-Path $directory 'seed-4207'
    if (-not (Test-Path -LiteralPath (Join-Path $seedDirectory 'metrics.json'))) {
        throw "No completed measurement evidence for $mode, player exit $($process.ExitCode)."
    }
    # A 60-second sample intentionally stops before the race result. Keep its
    # actual nonzero full-race suite exit, and validate performance separately.
    & python (Join-Path $PSScriptRoot 'analyze_performance.py') $seedDirectory | Set-Content -LiteralPath (Join-Path $directory 'analysis-summary.json') -Encoding UTF8
    if ($LASTEXITCODE -ne 0) { throw "Performance analysis failed for $mode." }
    Write-Output "Retained $mode sample; full-race suite exit=$($process.ExitCode)."
}
if ($Modes.Count -eq 2) {
    & python (Join-Path $PSScriptRoot 'compare_performance.py') $evidence --run-label $RunLabel
    if ($LASTEXITCODE -ne 0) { throw 'Pair comparison failed.' }
}
