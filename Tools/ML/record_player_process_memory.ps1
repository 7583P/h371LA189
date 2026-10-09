param(
    [Parameter(Mandatory=$true)][string]$OutputCsv,
    [int]$MaximumMinutes=20
)
$ErrorActionPreference='Stop'
$memoryWorkspace=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$memoryOutput=[IO.Path]::GetFullPath((Join-Path $memoryWorkspace $OutputCsv))
if (!$memoryOutput.StartsWith($memoryWorkspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'The output must stay within this workspace.' }
if (Test-Path -LiteralPath $memoryOutput) { throw 'Existing memory evidence is not overwritten.' }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($memoryOutput)) | Out-Null
'utc,pid,working_set_bytes,private_bytes,cpu_seconds' | Set-Content -LiteralPath $memoryOutput -Encoding utf8
$memoryDeadline=[DateTime]::UtcNow.AddMinutes($MaximumMinutes)
$memoryObserved=$false
while ([DateTime]::UtcNow -lt $memoryDeadline) {
    $memoryPlayers=@(Get-Process -Name 'HillAdventurer-HumanGate' -ErrorAction SilentlyContinue)
    if ($memoryPlayers.Count -eq 0) {
        if ($memoryObserved) { break }
    } else {
        $memoryObserved=$true
        foreach($memoryPlayer in $memoryPlayers) {
            $memoryLine=('{0},{1},{2},{3},{4}' -f [DateTime]::UtcNow.ToString('o'),$memoryPlayer.Id,$memoryPlayer.WorkingSet64,$memoryPlayer.PrivateMemorySize64,$memoryPlayer.TotalProcessorTime.TotalSeconds.ToString([Globalization.CultureInfo]::InvariantCulture))
            Add-Content -LiteralPath $memoryOutput -Value $memoryLine -Encoding utf8
        }
    }
    Start-Sleep -Seconds 5
}
Write-Output ('Observed process memory CSV: '+$memoryOutput)
