using LIS2.Runtime;

namespace LIS2.App.Avalonia.Tests;

public sealed class LinuxSystemSensorsTests
{
    [Fact]
    public void ReadsSystemMetricsAndComputesRatesFromSuccessiveSamples()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var proc = Path.Combine(root, "proc");
        var sys = Path.Combine(root, "sys");
        Directory.CreateDirectory(proc);
        Directory.CreateDirectory(Path.Combine(proc, "net"));
        Directory.CreateDirectory(Path.Combine(sys, "class/hwmon/hwmon0"));
        Directory.CreateDirectory(Path.Combine(sys, "devices/system/cpu/cpu0/cpufreq"));
        double now = 1;
        try
        {
            File.WriteAllText(Path.Combine(sys, "class/hwmon/hwmon0/name"), "pwmfan");
            File.WriteAllText(Path.Combine(sys, "class/hwmon/hwmon0/fan1_input"), "2800");
            File.WriteAllText(Path.Combine(sys, "devices/system/cpu/cpu0/cpufreq/scaling_cur_freq"), "1500000");
            File.WriteAllText(Path.Combine(proc, "meminfo"), "MemTotal: 1000 kB\nMemAvailable: 250 kB\nSwapTotal: 200 kB\nSwapFree: 100 kB\n");
            File.WriteAllText(Path.Combine(proc, "stat"), "cpu 100 0 100 800 0 0 0 0 0 0\ncpu0 100 0 100 800 0 0 0 0 0 0\n");
            File.WriteAllText(Path.Combine(proc, "diskstats"), " 259 0 nvme0n1 1 0 100 0 1 0 200 0 0 0 0 0 0 0 0\n");
            File.WriteAllText(Path.Combine(proc, "net/dev"), "Inter-| Receive | Transmit\n face |bytes packets errs drop fifo frame compressed multicast|bytes packets errs drop fifo colls carrier compressed\neth0: 1000 0 0 0 0 0 0 0 2000 0 0 0 0 0 0 0\n");
            var source = new LinuxSystemSensors(proc, sys, () => now, () => Array.Empty<DriveInfo>());
            var first = source.Snapshot();
            Assert.Equal(2800d, first["Hardware.pwmfan.Fan.fan1"]);
            Assert.Equal(1500d, first["Hardware.CPU.Clock.cpu0"]);
            Assert.Equal(768000d, first["Hardware.Memory.Data.Used"]);
            Assert.False(first.ContainsKey("Hardware.CPU.Load.Total"));
            now = 3;
            File.WriteAllText(Path.Combine(proc, "stat"), "cpu 150 0 150 900 0 0 0 0 0 0\ncpu0 150 0 150 900 0 0 0 0 0 0\n");
            File.WriteAllText(Path.Combine(proc, "diskstats"), " 259 0 nvme0n1 1 0 120 0 1 0 240 0 0 0 0 0 0 0 0\n");
            File.WriteAllText(Path.Combine(proc, "net/dev"), "Inter-| Receive | Transmit\n face |bytes packets errs drop fifo frame compressed multicast|bytes packets errs drop fifo colls carrier compressed\neth0: 1400 0 0 0 0 0 0 0 2600 0 0 0 0 0 0 0\n");
            var second = source.Snapshot();
            Assert.Equal(50d, second["Hardware.CPU.Load.Total"]);
            Assert.Equal(5120d, second["Hardware.Disk_nvme0n1.Throughput.Read"]);
            Assert.Equal(10240d, second["Hardware.Disk_nvme0n1.Throughput.Write"]);
            Assert.Equal(200d, second["Hardware.Network_eth0.Throughput.Receive"]);
            Assert.Equal(300d, second["Hardware.Network_eth0.Throughput.Send"]);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
