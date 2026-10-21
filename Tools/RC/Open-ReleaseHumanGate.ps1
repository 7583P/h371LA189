param(
    [string]$Player='Builds/ReleaseGates-20261019-01-Validated/HillAdventurer-HumanGate.exe',
    [string]$EvidenceRoot='Docs/ReleaseGates/20261019-01'
)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$playerPath=(Resolve-Path -LiteralPath (Join-Path $taskRoot $Player)).Path
$playerFolder=Split-Path -Parent $playerPath
$playerName=[IO.Path]::GetFileNameWithoutExtension($playerPath)
$identity=@(Get-Content -LiteralPath (Join-Path $playerFolder ($playerName+'_Data/app.info')))
if ($identity.Count -lt 2 -or $identity[0].Trim() -ne 'HillAdventurer.BlacklineQA') {throw 'Manual review requires the isolated BlacklineQA identity'}
if ((& git -C $taskRoot branch --show-current).Trim() -ne 'main') {throw 'Only main is authorized'}
$evidenceRootPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $EvidenceRoot))
$allowedEvidenceRoot=Join-Path $taskRoot 'Docs/ReleaseGates'
if (-not $evidenceRootPath.StartsWith($allowedEvidenceRoot+'\',[StringComparison]::OrdinalIgnoreCase)) {
    throw 'Evidence must stay in a release evidence directory in this workspace'
}
$runName='manual-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$evidence=Join-Path $evidenceRootPath $runName
$snapshot=Join-Path $taskRoot ('tmp/ReleaseGates/'+[IO.Path]::GetFileName($evidenceRootPath)+'/'+$runName+'-before')
$null=New-Item -ItemType Directory -Path $evidence,$snapshot
$registry='HKCU\Software\'+$identity[0].Trim()+'\'+$identity[1].Trim()
$registryPath='HKCU:\Software\'+$identity[0].Trim()+'\'+$identity[1].Trim()
if (Test-Path -LiteralPath $registryPath) {
    & reg.exe export $registry (Join-Path $snapshot 'qa-playerprefs.reg')
    if ($LASTEXITCODE -ne 0) {throw 'QA preferences backup failed'}
}
$qaSave=Join-Path $env:USERPROFILE ('AppData/LocalLow/'+$identity[0].Trim()+'/'+$identity[1].Trim()+'/LocalRacingLeague')
if (Test-Path -LiteralPath $qaSave) {Copy-Item -LiteralPath $qaSave -Destination $snapshot -Recurse}
@{classification='PENDING_MANUAL_REVIEW_NOT_APPROVAL';utc=[DateTime]::UtcNow.ToString('o');
    playerSha256=(Get-FileHash -LiteralPath $playerPath).Hash;ml='Off';rainbowPreview=$true;
    company=$identity[0].Trim();save='fresh --closure-output/isolated-product-save';snapshot=$snapshot} |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'started.json') -Encoding UTF8
$arguments=@('-screen-width','1600','-screen-height','900','-screen-fullscreen','0','--rainbow-preview',
    '--racing-ml-mode','Off','--closure-output',$evidence,'-logFile',(Join-Path $evidence 'player.log'))
$argumentLine=($arguments | ForEach-Object {'"'+$_.Replace('"','\"')+'"'}) -join ' '
Write-Output 'HumanGate: jugar manualmente y registrar la revision. Abrir el juego no implica aprobarlo.'
# A visible window is intentional: this entry point is exclusively for manual play.
Start-Process -FilePath $playerPath -ArgumentList $argumentLine -WorkingDirectory $taskRoot -WindowStyle Normal
