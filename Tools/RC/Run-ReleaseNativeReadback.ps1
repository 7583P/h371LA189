param(
    [Parameter(Mandatory=$true)][string]$Player,
    [Parameter(Mandatory=$true)][string]$SourceSave,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$Seed=4301
)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
if ((& git -C $taskRoot branch --show-current).Trim() -ne 'main') {throw 'Only main is authorized'}
$playerPath=(Resolve-Path -LiteralPath (Join-Path $taskRoot $Player)).Path
$savePath=(Resolve-Path -LiteralPath (Join-Path $taskRoot $SourceSave)).Path
$outputPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
if (-not $savePath.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or -not $outputPath.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Use workspace QA paths only'}
if (Test-Path -LiteralPath $outputPath) {throw 'Never overwrite readback evidence'}
$folder=Split-Path -Parent $playerPath
$name=[IO.Path]::GetFileNameWithoutExtension($playerPath)
$identity=@(Get-Content -LiteralPath (Join-Path $folder ($name+'_Data/app.info')))
if($identity[0].Trim() -ne 'HillAdventurer.BlacklineQA'){throw 'A proven isolated QA player is required'}
$null=New-Item -ItemType Directory -Path $outputPath
$evidenceRun=[IO.Path]::GetFileName((Split-Path -Parent $outputPath))
$snapshot=Join-Path $taskRoot ('tmp/ReleaseGates/'+$evidenceRun+'/native-readback-before-'+[Guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory -Path $snapshot
$registry='HKCU:\Software\'+$identity[0].Trim()+'\'+$identity[1].Trim()
if(Test-Path -LiteralPath $registry){
    & reg.exe export ('HKCU\Software\'+$identity[0].Trim()+'\'+$identity[1].Trim()) (Join-Path $snapshot 'qa-playerprefs.reg')
    if($LASTEXITCODE -ne 0){throw 'QA preference snapshot failed'}
}
function Save-Hashes {return @(Get-ChildItem -LiteralPath $savePath -Recurse -File | Sort-Object FullName | ForEach-Object {[ordered]@{path=$_.FullName.Substring($savePath.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})}
$before=Save-Hashes
Copy-Item -LiteralPath $savePath -Destination (Join-Path $outputPath 'isolated-product-save') -Recurse
$manifest=[ordered]@{classification='FRESH_NATIVE_UI_READBACK_FROM_COPY_OF_EXISTING_PHYSICAL_QA_SAVE';seed=$Seed;source=$savePath;before=$before;qaSnapshot=$snapshot;playerSha256=(Get-FileHash -LiteralPath $playerPath).Hash;assemblySha256=(Get-FileHash -LiteralPath (Join-Path $folder ($name+'_Data/Managed/Assembly-CSharp.dll'))).Hash}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'started.json') -Encoding UTF8
$arguments=@('-batchmode','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','--league-readback-player','--league-readback-seed',"$Seed",'--racing-ml-mode','Off','--closure-output',$outputPath,'-logFile',($outputPath+'.log'))
$line=($arguments | ForEach-Object {'"'+$_+'"'}) -join ' '
$process=Start-Process -FilePath $playerPath -ArgumentList $line -WorkingDirectory $taskRoot -WindowStyle Hidden -Wait -PassThru
$manifest.exitCode=$process.ExitCode;$manifest.after=Save-Hashes
$manifest.sourceUnchanged=($before | ConvertTo-Json -Depth 5 -Compress) -eq ($manifest.after | ConvertTo-Json -Depth 5 -Compress)
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'completed.json') -Encoding UTF8
if($process.ExitCode -ne 0 -or -not $manifest.sourceUnchanged){throw 'Native readback failed; retain all evidence'}
