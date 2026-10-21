param(
    [Parameter(Mandatory=$true)][string]$Player,
    [Parameter(Mandatory=$true)][string]$Output,
    [int[]]$Ratings = @(9700,10000),
    [int[]]$Seeds = @(4207,4208,4209),
    [switch]$RainbowPreview,
    [ValidateSet('profile','final-qa')][string]$Mode='profile'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$playerPath = (Resolve-Path -LiteralPath $Player).Path
$outputPath = [IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
if (-not $outputPath.StartsWith($taskRoot + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence must stay in this workspace' }
if (Test-Path -LiteralPath $outputPath) { throw 'Refusing to overwrite race evidence' }
if ((& git -C $taskRoot branch --show-current).Trim() -ne 'main') { throw 'Only main is authorized' }
$null = New-Item -ItemType Directory -Path $outputPath
$playerFolder = Split-Path -Parent $playerPath
$playerName = [IO.Path]::GetFileNameWithoutExtension($playerPath)
$appInfo = Join-Path $playerFolder ($playerName + '_Data/app.info')
$identity = @(Get-Content -LiteralPath $appInfo)
if ($identity.Count -lt 2 -or $identity[0].Trim() -ne 'HillAdventurer.BlacklineQA') {
    throw 'Only a proven BlacklineQA player is allowed; production persistence is forbidden'
}
# Unity namespaces PlayerPrefs and persistentDataPath by the baked company/product.
# RacingProgressionService additionally routes its store into --closure-output/isolated-product-save.
$qaSaveRoot = Join-Path $env:USERPROFILE ('AppData/LocalLow/' + $identity[0].Trim() + '/' + $identity[1].Trim())
$snapshotRoot = Join-Path $taskRoot ('tmp/ReleaseGates/' + [IO.Path]::GetFileName($outputPath) + '-qa-before-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $snapshotRoot
if (Test-Path -LiteralPath $qaSaveRoot) {
    foreach ($folderName in @('LocalRacingLeague')) {
        $sourceFolder = Join-Path $qaSaveRoot $folderName
        if (Test-Path -LiteralPath $sourceFolder) { Copy-Item -LiteralPath $sourceFolder -Destination $snapshotRoot -Recurse }
    }
}
$qaRegistry = 'HKCU:\Software\' + $identity[0].Trim() + '\' + $identity[1].Trim()
if (Test-Path -LiteralPath $qaRegistry) {
    & reg.exe export ('HKCU\Software\' + $identity[0].Trim() + '\' + $identity[1].Trim()) (Join-Path $snapshotRoot 'qa-playerprefs.reg')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot snapshot QA preferences; do not start player' }
}
$artifactPaths = @($playerPath,(Join-Path $playerFolder 'UnityPlayer.dll'),(Join-Path $playerFolder ($playerName + '_Data/Managed/Assembly-CSharp.dll')))
function Artifact-Hashes {
    $rows = @()
    foreach ($artifact in $artifactPaths) { $rows += @{path=$artifact;sha256=(Get-FileHash -LiteralPath $artifact).Hash} }
    return $rows
}
$manifest = [ordered]@{
    classification='REAL_RENDERED_UNITY_PHYSICS_SCRIPTED_HUMAN_NOT_MANUAL_APPROVAL'
    startedUtc=[DateTime]::UtcNow.ToString('o'); head=(& git -C $taskRoot rev-parse HEAD)
    artifactsBefore=Artifact-Hashes; ratings=$Ratings; seeds=$Seeds
    timeScale=1; ml='Off'; resolution='1600x900'; quality='High'
    rainbowPreview=[bool]$RainbowPreview
    mode=$Mode
    focusRequiredForPerformance=$true; performanceClaim=$false; runs=@()
    isolation=@{bakedCompany=$identity[0].Trim();bakedProduct=$identity[1].Trim();qaPersistentDataPath=$qaSaveRoot;leaguePath='per-run --closure-output/isolated-product-save';snapshot=$snapshotRoot;productionCompany='DefaultCompany';productionDataAccess=$false}
}
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $outputPath 'started.json') -Encoding UTF8
foreach ($rating in $Ratings) {
    $directory = Join-Path $outputPath ('mmr-' + $rating)
    $arguments = @('-batchmode','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-force-d3d11',
        '--closure-player','--closure-mode',$Mode,'--closure-seeds',($Seeds -join ','),'--closure-mmr',"$rating",
        '--closure-timescale','1','--racing-ml-mode','Off','--buffered-driving-traces','--elite-forensics','--closure-capture',
        '--closure-output',$directory,'-logFile',($directory + '.log'))
    if ($RainbowPreview) { $arguments += '--rainbow-preview' }
    $argumentLine = ($arguments | ForEach-Object { '"' + $_.Replace('"','\"') + '"' }) -join ' '
    Write-Output "Running MMR $rating, seeds $($Seeds -join ','), ML Off, scale 1."
    $process = Start-Process -FilePath $playerPath -ArgumentList $argumentLine -WorkingDirectory $taskRoot -WindowStyle Hidden -Wait -PassThru
    $manifest.runs += @{mmr=$rating;pid=$process.Id;exitCode=$process.ExitCode;arguments=$arguments}
    $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $outputPath 'progress.json') -Encoding UTF8
    if ($process.ExitCode -ne 0) { Write-Warning "Retained failing race run, exit $($process.ExitCode)." }
}
$manifest.artifactsAfter = Artifact-Hashes
$manifest.finishedUtc = [DateTime]::UtcNow.ToString('o')
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $outputPath 'completed.json') -Encoding UTF8
