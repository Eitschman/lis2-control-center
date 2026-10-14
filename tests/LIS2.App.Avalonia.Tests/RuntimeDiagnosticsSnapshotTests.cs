using LIS2.App.Avalonia;

namespace LIS2.App.Avalonia.Tests;

public sealed class RuntimeDiagnosticsSnapshotTests
{
    [Fact]
    public void DisconnectedSnapshotIsReadOnlyAndReportsSafeStates()
    {
        var text = RuntimeDiagnosticsSnapshot.Format("Linux", false, false, false, false, 0);
        Assert.Contains("Virtual transport: Disconnected", text);
        Assert.Contains("Serial transport: Disconnected", text);
        Assert.Contains("Fan dry-run monitor: Stopped", text);
        Assert.Contains("Physical fan authorization: Not displayed", text);
        Assert.Contains("Hardware sensor live refresh: Stopped", text);
        Assert.Contains("Serial operation: Idle", text);
    }

    [Fact]
    public void ActiveSnapshotReflectsStateWithoutClaimingAuthorization()
    {
        var text = RuntimeDiagnosticsSnapshot.Format("Windows", true, true, true, true, 5, true, true, 3);
        Assert.Contains("Virtual commands: 5", text);
        Assert.Contains("Fan dry-run monitor: Running", text);
        Assert.Contains("Serial operation: Busy", text);
        Assert.Contains("Hardware sensor live refresh: Running", text);
        Assert.Contains("Configured fan sensor channels: 3", text);
        Assert.DoesNotContain("Physical fan authorization: Enabled", text);
    }
}
