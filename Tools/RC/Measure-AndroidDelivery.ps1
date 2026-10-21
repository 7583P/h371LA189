param(
    [Parameter(Mandatory=$true)][string]$Apks,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$Bundle,
    [switch]$CreateApkSet
)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$androidTools='C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$java=Join-Path $androidTools 'OpenJDK/bin/java.exe'
$bundletool=Join-Path $androidTools 'Tools/bundletool-all-1.17.2.jar'
$outputPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
$apksPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $Apks))
if (-not $outputPath.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Evidence must stay in this workspace'}
if (Test-Path -LiteralPath $outputPath) {throw 'Preserve prior delivery evidence'}
$null=New-Item -ItemType Directory -Path $outputPath
$manifest=[ordered]@{classification='BUNDLETOOL_ESTIMATED_COMPRESSED_DOWNLOAD_NOT_INSTALLED_SIZE_OR_PLAY_DEVICE_APPROVAL';utc=[DateTime]::UtcNow.ToString('o');
    bundletoolSha256=(Get-FileHash -LiteralPath $bundletool).Hash;apks=$apksPath;bundle=$Bundle;productionSigned=$false;deviceAttached=$false;reference='ARM64 / API35 / density420 / en';results=@()}
if ($CreateApkSet) {
    if (-not $apksPath.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $apksPath)) {throw 'Only a new APK set inside this workspace is allowed'}
    if (-not $Bundle) {throw 'A forensic bundle is required'}
    $bundlePath=(Resolve-Path -LiteralPath (Join-Path $taskRoot $Bundle)).Path
    $debugKey=Join-Path $env:USERPROFILE '.android/debug.keystore'
    if (-not (Test-Path -LiteralPath $debugKey)) {throw 'Existing default debug identity required; never create or use a production signer'}
    $manifest.debugSignerBefore=(Get-FileHash -LiteralPath $debugKey).Hash
    # The fixed Android debug password is public, not a production credential.
    # This command reads the existing debug identity and never modifies it.
    & $java -jar $bundletool build-apks ('--bundle='+$bundlePath) ('--output='+$apksPath) ('--ks='+$debugKey) --ks-key-alias=androiddebugkey --ks-pass=pass:android --key-pass=pass:android *> (Join-Path $outputPath 'build-apks.txt')
    if ($LASTEXITCODE -ne 0) {throw 'APK-set creation failed; log retained'}
    $manifest.debugSignerAfter=(Get-FileHash -LiteralPath $debugKey).Hash
    if ($manifest.debugSignerBefore -ne $manifest.debugSignerAfter) {throw 'Debug identity changed unexpectedly'}
    $manifest.bundleSha256=(Get-FileHash -LiteralPath $bundlePath).Hash
    $manifest.bundleBytes=(Get-Item -LiteralPath $bundlePath).Length
}
$manifest.apksSha256=(Get-FileHash -LiteralPath $apksPath).Hash
$manifest.apksBytes=(Get-Item -LiteralPath $apksPath).Length
foreach ($density in @(160,240,320,420,480,560,640)) {
    $specPath=Join-Path $outputPath ('arm64-api35-density'+$density+'.json')
    @{supportedAbis=@('arm64-v8a');supportedLocales=@('en');screenDensity=$density;sdkVersion=35} | ConvertTo-Json | Set-Content -LiteralPath $specPath -Encoding UTF8
    $csvPath=Join-Path $outputPath ('delivery-'+$density+'.csv')
    & $java -jar $bundletool get-size total ('--apks='+$apksPath) ('--device-spec='+$specPath) *> $csvPath
    if ($LASTEXITCODE -ne 0) {throw "Delivery measurement failed for density $density"}
    $rows=@(Import-Csv -LiteralPath $csvPath)
    if ($rows.Count -ne 1 -or -not $rows[0].MAX) {throw "Unexpected bundletool result for density $density"}
    $manifest.results+=@{abi='arm64-v8a';sdk=35;density=$density;minimumBytes=[long]$rows[0].MIN;maximumBytes=[long]$rows[0].MAX;marginBytes=150000000-[long]$rows[0].MAX;underTarget=([long]$rows[0].MAX -le 150000000)}
}
$manifest.unsupportedAbis='Only ARM64 is in the authored configuration; ARMv7/x86 are not delivery targets'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'delivery-summary.json') -Encoding UTF8
$manifest.results | ForEach-Object {[pscustomobject]$_} | Format-Table abi,density,maximumBytes,marginBytes,underTarget
