param(
    [Parameter(Mandatory = $true)][string]$AppDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$sourceDirectory = (Resolve-Path -LiteralPath $AppDirectory).Path
$evidenceDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path -LiteralPath (Join-Path $sourceDirectory 'LIS2.App.Avalonia.exe'))) { throw 'Published application missing' }
if (Test-Path -LiteralPath $evidenceDirectory) { throw 'Updater smoke requires a new output directory' }
New-Item -ItemType Directory -Path $evidenceDirectory | Out-Null

# Beide Fälle verwenden ausschließlich eigene Verzeichnisse und isolierte Desktop-Workspaces.
foreach ($scenario in @('success', 'rollback')) {
    $caseDirectory = Join-Path $evidenceDirectory $scenario
    New-Item -ItemType Directory -Path $caseDirectory | Out-Null
    $targetDirectory = Join-Path $caseDirectory 'installed'
    $identifier = [Guid]::NewGuid().ToString('N')
    $stageDirectory = Join-Path $caseDirectory ('.lis2-update-' + $identifier)
    $previousDirectory = Join-Path $caseDirectory ('.lis2-previous-' + $identifier)
    $sessionDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ('LIS2ControlCenter\updates\' + [Guid]::NewGuid().ToString('N'))
    $helperDirectory = Join-Path $sessionDirectory 'helper'
    foreach ($destination in @($targetDirectory, $stageDirectory, $helperDirectory)) {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        foreach ($entry in Get-ChildItem -LiteralPath $sourceDirectory) { Copy-Item -LiteralPath $entry.FullName -Destination $destination -Recurse }
    }
    Set-Content -LiteralPath (Join-Path $targetDirectory 'update-marker.txt') -Value 'old'
    Set-Content -LiteralPath (Join-Path $stageDirectory 'update-marker.txt') -Value 'new'
    if ($scenario -eq 'rollback') {
        # Eine fehlende Programmdatei prüft den Neustartfehler ohne interaktiven Windows-Systemdialog.
        [IO.File]::Delete((Join-Path $stageDirectory 'LIS2.App.Avalonia.exe'))
    }
    $previousProcess = Start-Process 'powershell.exe' -ArgumentList '-NoProfile', '-Command', 'Start-Sleep -Seconds 2' -PassThru -WindowStyle Hidden
    $plan = @{
        Target = $targetDirectory; Stage = $stageDirectory; Backup = $previousDirectory; Session = $sessionDirectory
        ProcessId = $previousProcess.Id; ProcessStartTicks = $previousProcess.StartTime.ToUniversalTime().Ticks; DesktopSmoke = $true
    }
    $planFile = Join-Path $sessionDirectory 'install.json'
    [IO.File]::WriteAllText($planFile, ($plan | ConvertTo-Json))
    [IO.File]::WriteAllText((Join-Path $sessionDirectory 'armed'), 'isolated test process')
    $helperFile = Join-Path $helperDirectory 'LIS2.App.Avalonia.exe'
    $helperProcess = Start-Process -FilePath $helperFile -ArgumentList '--apply-update', ('"' + $planFile + '"') -WorkingDirectory $sessionDirectory -PassThru
    if (-not $helperProcess.WaitForExit(150000)) { $helperProcess.Kill(); throw "Updater timed out: $scenario" }
    $expectedCode = if ($scenario -eq 'success') { 0 } else { 1 }
    if ($helperProcess.ExitCode -ne $expectedCode) { throw "Unexpected updater exit: $scenario / $($helperProcess.ExitCode)" }
    $marker = (Get-Content -LiteralPath (Join-Path $targetDirectory 'update-marker.txt') -Raw).Trim()
    $expectedMarker = if ($scenario -eq 'success') { 'new' } else { 'old' }
    if ($marker -ne $expectedMarker) { throw "Wrong installation after $scenario" }
    $desktopDirectory = Join-Path $sessionDirectory $(if ($scenario -eq 'success') { 'desktop-smoke' } else { 'rollback-smoke' })
    $deadline = [DateTime]::UtcNow.AddSeconds(100)
    while (-not (Test-Path -LiteralPath (Join-Path $desktopDirectory 'result.txt'))) {
        if ([DateTime]::UtcNow -gt $deadline) { throw "Restarted desktop failed: $scenario / $sessionDirectory" }
        Start-Sleep -Milliseconds 200
    }
    Copy-Item -LiteralPath (Join-Path $sessionDirectory 'result.txt') -Destination (Join-Path $caseDirectory 'update-result.txt')
    Copy-Item -LiteralPath $desktopDirectory -Destination (Join-Path $caseDirectory 'desktop') -Recurse
    Write-Output "PASS: $scenario, real updater process, installation marker, desktop startup and shutdown"
}
