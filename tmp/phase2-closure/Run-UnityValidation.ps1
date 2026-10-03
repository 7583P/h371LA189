param(
    [Parameter(Mandatory = $true)][string]$Label,
    [string]$Mode = 'races',
    [string]$Seeds = '4207',
    [float]$TimeScale = 4,
    [int]$Mmr = 1000,
    [switch]$Capture,
    [switch]$Headless,
    [switch]$RestoreOnly
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
public static class Phase2RegistryBytes {
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegQueryValueEx(IntPtr key, string name, IntPtr reserved, out uint type, byte[] data, ref uint length);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegSetValueEx(IntPtr key, string name, uint reserved, uint type, byte[] data, uint length);
    public static byte[] Read(RegistryKey key, string name) {
        uint kind, length = 0;
        int error = RegQueryValueEx(key.Handle.DangerousGetHandle(), name, IntPtr.Zero, out kind, null, ref length);
        if (error != 0) throw new Exception("RegQueryValueEx failed: " + error);
        byte[] data = new byte[length];
        error = RegQueryValueEx(key.Handle.DangerousGetHandle(), name, IntPtr.Zero, out kind, data, ref length);
        if (error != 0) throw new Exception("RegQueryValueEx failed: " + error);
        return data;
    }
    public static void Write(RegistryKey key, string name, int kind, byte[] data) {
        int error = RegSetValueEx(key.Handle.DangerousGetHandle(), name, 0, (uint)kind, data, (uint)data.Length);
        if (error != 0) throw new Exception("RegSetValueEx failed: " + error);
    }
}
'@
$validationRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$unityExecutable = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
if ((Get-Content -LiteralPath (Join-Path $validationRoot 'ProjectSettings\ProjectVersion.txt'))[0] -ne 'm_EditorVersion: 6000.3.13f1') { throw 'Unexpected Unity version.' }
if (!(Test-Path -LiteralPath (Join-Path $validationRoot 'Assets\AnimeHillRacing\Scripts\Racing3D'))) { throw 'Racing3D is missing.' }
if ($Label -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a simple, unique validation label.' }
$validationOutput = Join-Path $validationRoot ('Logs\RacingPhase2\closure\' + $Label)
function Restore-ProtectedSave($savedSnapshots) {
    $restoredCount = 0
    foreach ($snapshot in $savedSnapshots) {
        $restoreKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($snapshot.Path)
        try {
            $originalNames = @($snapshot.Values | ForEach-Object { $_.Name })
            foreach ($valueName in $restoreKey.GetValueNames()) {
                if ($originalNames -notcontains $valueName) { $restoreKey.DeleteValue($valueName, $false) }
            }
            foreach ($value in $snapshot.Values) {
                if ($value.PSObject.Properties.Name -contains 'Data') {
                    $rawData = [byte[]]$value.Data
                } else {
                    # Unity stores some 8-byte floating point values with a DWORD kind.
                    # Preserve their exact bytes rather than converting them to Int32.
                    $rawData = switch ([int]$value.Kind) {
                        3 { ,([byte[]]$value.Value) }
                        4 {
                            if ($value.Value -is [long]) { ,([BitConverter]::GetBytes([long]$value.Value)) }
                            else { ,([BitConverter]::GetBytes([int]$value.Value)) }
                        }
                        11 { ,([BitConverter]::GetBytes([long]$value.Value)) }
                        default { throw 'Unsupported kind in the initial legacy save snapshot.' }
                    }
                }
                [Phase2RegistryBytes]::Write($restoreKey, $value.Name, [int]$value.Kind, [byte[]]$rawData)
                $actual = [Phase2RegistryBytes]::Read($restoreKey, $value.Name)
                if ([int]$restoreKey.GetValueKind($value.Name) -ne [int]$value.Kind) { throw 'Save value kind differs after restoration.' }
                if ([Convert]::ToBase64String([byte[]]$actual) -ne [Convert]::ToBase64String([byte[]]$rawData)) { throw 'Save bytes differ after restoration.' }
                $restoredCount++
            }
            if ($restoreKey.GetValueNames().Count -ne $snapshot.Values.Count) { throw 'Save value count differs after restoration.' }
        } finally { $restoreKey.Dispose() }
    }
    return $restoredCount
}
if ($RestoreOnly) {
    $restoreSnapshots = Import-Clixml -LiteralPath (Join-Path $validationOutput 'save-before.clixml')
    $restoredCount = Restore-ProtectedSave $restoreSnapshots
    [PSCustomObject]@{ Restored = $true; Verified = $true; Values = $restoredCount; RecoveryOfFailedRestore = $true } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $validationOutput 'save-restored.json') -Encoding UTF8
    Write-Output ('Restored and verified PlayerPrefs: ' + $restoredCount + ' values.')
    exit 0
}
if (Test-Path -LiteralPath $validationOutput) { throw 'An existing evidence directory will not be overwritten.' }
New-Item -ItemType Directory -Path $validationOutput -Force | Out-Null
$savePaths = @('Software\Unity\UnityEditor\DefaultCompany\Hill Adventurer', 'Software\DefaultCompany\Hill Adventurer')
$snapshots = foreach ($savePath in $savePaths) {
    $saveKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($savePath)
    $saveValues = @()
    if ($null -ne $saveKey) {
        try {
            $saveValues = @(foreach ($valueName in $saveKey.GetValueNames()) {
                [PSCustomObject]@{ Name = $valueName; Data = [Phase2RegistryBytes]::Read($saveKey, $valueName); Kind = [int]$saveKey.GetValueKind($valueName) }
            })
        } finally { $saveKey.Dispose() }
    }
    [PSCustomObject]@{ Path = $savePath; Values = $saveValues }
}
$snapshots | Export-Clixml -LiteralPath (Join-Path $validationOutput 'save-before.clixml')
Write-Output ('Protected PlayerPrefs: ' + ($snapshots.Values | Measure-Object).Count + ' values.')
$unityArguments = @('-batchmode','-force-d3d11','-projectPath',('"' + $validationRoot + '"'),'-buildTarget','Win64','-logFile',('"' + (Join-Path $validationOutput 'unity.log') + '"'),'-executeMethod','AnimeHillRacing.EditorTools.RacingClosureValidation.Run','--closure-output',('"' + $validationOutput + '"'),'--closure-mode',$Mode,'--closure-seeds',$Seeds,'--closure-timescale',$TimeScale.ToString([Globalization.CultureInfo]::InvariantCulture),'--closure-mmr',$Mmr)
if ($Capture) { $unityArguments += '--closure-capture' }
if ($Headless) {
    if ($Mode -notin @('races', 'benchmark') -or $Capture) { throw 'Headless runs are limited to uncaptured Editor races and benchmarks.' }
    $unityArguments += '-nographics'
}
if ($Mode -eq 'profile-player' -or $Mode -eq 'profile-rendered' -or $Mode -eq 'manual-player') {
    $unityExecutable = Join-Path $validationRoot 'Builds\Phase2Closure\HillAdventurerValidation.exe'
    if (!(Test-Path -LiteralPath $unityExecutable)) { throw 'Build the development player before profiling.' }
    $unityArguments = @('-batchmode','-force-d3d11','-screen-width','1600','-screen-height','900','-logFile',('"' + (Join-Path $validationOutput 'player.log') + '"'),'--closure-player','--closure-output',('"' + $validationOutput + '"'),'--closure-mode','profile','--closure-seeds',$Seeds,'--closure-timescale',$TimeScale.ToString([Globalization.CultureInfo]::InvariantCulture),'--closure-mmr',$Mmr)
    if ($Mode -eq 'profile-rendered') {
        $unityArguments = @($unityArguments | Where-Object { $_ -ne '-batchmode' }) + @('-screen-fullscreen','0')
    }
    if ($Mode -eq 'manual-player') {
        $unityArguments = @('-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',('"' + (Join-Path $validationOutput 'player.log') + '"'))
    }
    if ($Capture) { $unityArguments += '--closure-capture' }
}
$validationExit = 1
try {
    if ($Mode -eq 'profile-rendered' -or $Mode -eq 'manual-player') {
        # Render the full player workload without taking over the user's desktop.
        $validationProcess = Start-Process -FilePath $unityExecutable -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
    } else {
        $validationProcess = Start-Process -FilePath $unityExecutable -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
    }
    Write-Output ('Unity validation PID: ' + $validationProcess.Id)
    $validationTimeoutMs = if ($Mode -eq 'benchmark') { 2400000 } else { 1200000 }
    if (!$validationProcess.WaitForExit($validationTimeoutMs)) {
        $validationProcess.Kill()
        $validationProcess.WaitForExit()
        throw 'The validation exceeded its time limit; only its own Unity process was stopped.'
    }
    $validationExit = $validationProcess.ExitCode
} finally {
    $restoredCount = Restore-ProtectedSave $snapshots
    [PSCustomObject]@{ Restored = $true; Verified = $true; Values = $restoredCount; UnityExit = $validationExit } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $validationOutput 'save-restored.json') -Encoding UTF8
    Write-Output ('Restored PlayerPrefs: ' + $restoredCount + ' values.')
}
Write-Output ('Unity exit code: ' + $validationExit)
exit $validationExit
