param([Parameter(Mandatory=$true)][string]$Source, [Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference = 'Stop'
$content = [System.IO.File]::ReadAllText($Source)
$match = [regex]::Match($content, 'private const string Lis2IconBase64\s*=\s*"([A-Za-z0-9+/=]+)";')
if (-not $match.Success) { throw 'Embedded LIS2 icon was not found.' }
$bytes = [Convert]::FromBase64String($match.Groups[1].Value)
$directory = [System.IO.Path]::GetDirectoryName($Destination)
[System.IO.Directory]::CreateDirectory($directory) | Out-Null
[System.IO.File]::WriteAllBytes($Destination, $bytes)
