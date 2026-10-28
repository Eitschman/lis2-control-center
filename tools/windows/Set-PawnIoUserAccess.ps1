# Einmalige, ausdrücklich beauftragte PawnIO-Benutzerfreigabe mit gesichertem Rückweg.
# Der Zugriff gilt für alle Prozesse des Zielkontos und ist nicht auf Temperatursensoren beschränkt.
[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(Mandatory=$true)][string]$UserSid,
    [Parameter(Mandatory=$true)][string]$StatePath,
    [string]$DeviceInstanceId = 'ROOT\PAWNIO\0000',
    [switch]$Restore
)
$ErrorActionPreference = 'Stop'
if ($UserSid -notmatch '^S-1-5-21-\d+-\d+-\d+-\d+$') { throw 'A specific local/domain user SID is required.' }
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Elevation is required.' }
if ($DeviceInstanceId -notmatch '^ROOT\\PAWNIO\\[0-9A-Fa-f]{4}$') { throw 'An explicit PawnIO device instance is required.' }
$deviceId = $DeviceInstanceId
$device = Get-PnpDevice -InstanceId $deviceId
if ($device.FriendlyName -ne 'PawnIO') { throw 'Unexpected device.' }
$originalPath = [IO.Path]::GetFullPath($StatePath)
if (-not $Restore -and (Test-Path -LiteralPath $originalPath)) { throw 'Original security descriptor is already saved; refusing to overwrite it.' }
if (-not ('PawnDeviceSecurity' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class PawnDeviceSecurity {
    [StructLayout(LayoutKind.Sequential)] public struct DeviceInfo { public int Size; public Guid ClassGuid; public uint DevInst; public IntPtr Reserved; }
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr SetupDiGetClassDevsW(IntPtr classGuid, string enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiOpenDeviceInfoW(IntPtr set, string instance, IntPtr parent, uint flags, ref DeviceInfo info);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr set, ref DeviceInfo info, uint property, out uint type, byte[] buffer, uint length, out uint needed);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiSetDeviceRegistryPropertyW(IntPtr set, ref DeviceInfo info, uint property, byte[] buffer, uint length);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    public static byte[] Read(string instance) { return Access(instance, null); }
    public static void Write(string instance, byte[] bytes) { Access(instance, bytes); }
    static byte[] Access(string instance, byte[] replacement) {
        IntPtr set = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, 6);
        if (set == new IntPtr(-1)) throw new Win32Exception();
        try {
            var info = new DeviceInfo { Size = Marshal.SizeOf(typeof(DeviceInfo)) };
            if (!SetupDiOpenDeviceInfoW(set, instance, IntPtr.Zero, 0, ref info)) throw new Win32Exception();
            if (replacement != null) {
                if (!SetupDiSetDeviceRegistryPropertyW(set, ref info, 0x17, replacement, (uint)replacement.Length)) throw new Win32Exception();
                return replacement;
            }
            uint type, needed;
            SetupDiGetDeviceRegistryPropertyW(set, ref info, 0x17, out type, null, 0, out needed);
            if (needed == 0) throw new Win32Exception();
            byte[] buffer = new byte[needed];
            if (!SetupDiGetDeviceRegistryPropertyW(set, ref info, 0x17, out type, buffer, needed, out needed)) throw new Win32Exception();
            return buffer;
        } finally { SetupDiDestroyDeviceInfoList(set); }
    }
}
'@
}
$original = [PawnDeviceSecurity]::Read($deviceId)
$descriptor = [Security.AccessControl.RawSecurityDescriptor]::new($original, 0)
$originalSddl = $descriptor.GetSddlForm([Security.AccessControl.AccessControlSections]::All)
if ($null -eq $descriptor.DiscretionaryAcl) { throw 'Unexpected unrestricted DACL.' }
if ($Restore) {
    $state = Get-Content -LiteralPath $originalPath -Raw | ConvertFrom-Json
    if ($state.DeviceId -ne $deviceId -or $state.UserSid -ne $UserSid) { throw 'Saved device/user does not match the requested target.' }
    if ($state.UpdatedSddl -ne $originalSddl) { throw 'Device permissions have changed since the grant; refusing to overwrite them.' }
    $updated = [Convert]::FromBase64String($state.Descriptor)
    $expectedSddl = $state.Sddl
} else {
    $sid = [Security.Principal.SecurityIdentifier]::new($UserSid)
    foreach ($entry in $descriptor.DiscretionaryAcl) { if ($entry.SecurityIdentifier -eq $sid) { throw 'A user ACE already exists; review it first.' } }
    $accessMask = -1073741824 # GENERIC_READ | GENERIC_WRITE, erforderlich für den LibreHardwareMonitor-Gerätezugriff.
    $ace = [Security.AccessControl.CommonAce]::new([Security.AccessControl.AceFlags]::None, [Security.AccessControl.AceQualifier]::AccessAllowed, $accessMask, $sid, $false, $null)
    $descriptor.DiscretionaryAcl.InsertAce($descriptor.DiscretionaryAcl.Count, $ace)
    $updated = New-Object byte[] $descriptor.BinaryLength
    $descriptor.GetBinaryForm($updated, 0)
    $expectedSddl = $descriptor.GetSddlForm([Security.AccessControl.AccessControlSections]::All)
}
if (-not $PSCmdlet.ShouldProcess($deviceId, $(if ($Restore) { 'Restore saved PawnIO device permissions' } else { 'Grant PawnIO read/write access only to ' + $UserSid }))) { return }
if (-not $Restore) {
    @{ DeviceId=$deviceId; UserSid=$UserSid; Sddl=$originalSddl; UpdatedSddl=$expectedSddl; Descriptor=[Convert]::ToBase64String($original); TimestampUtc=[DateTime]::UtcNow.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $originalPath -Encoding UTF8
}
[PawnDeviceSecurity]::Write($deviceId, $updated)
$check = [Security.AccessControl.RawSecurityDescriptor]::new([PawnDeviceSecurity]::Read($deviceId), 0)
if ($check.GetSddlForm([Security.AccessControl.AccessControlSections]::All) -ne $expectedSddl) { throw 'Device security readback mismatch.' }
$restartOutput = & pnputil.exe /restart-device $deviceId 2>&1
$restartCode = $LASTEXITCODE
@{ DeviceId=$deviceId; UserSid=$UserSid; Before=$originalSddl; After=$check.GetSddlForm([Security.AccessControl.AccessControlSections]::All); RestartExitCode=$restartCode; RestartOutput=($restartOutput -join "`n") } | ConvertTo-Json | Set-Content -LiteralPath ($originalPath + '.result.json') -Encoding UTF8
if ($restartCode -ne 0) { throw "Device restart requires attention: $restartCode" }
