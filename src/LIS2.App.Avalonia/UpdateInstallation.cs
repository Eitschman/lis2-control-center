using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace LIS2.App.Avalonia;

/// <summary>Übergibt Debian-Pakete an den Desktop; portable Windows-Updates laufen außerhalb des Installationsordners.</summary>
internal static class UpdateInstallation
{
    internal static bool DebianAvailable => OperatingSystem.IsLinux() && File.Exists("/usr/bin/dpkg");

    internal static async Task OpenLinuxPackageAsync(string path, CancellationToken cancellationToken, Func<string, ProcessStartInfo>? createOpener = null)
    {
        if (!OperatingSystem.IsLinux() || !path.EndsWith(".deb", StringComparison.Ordinal) || !File.Exists(path))
            throw new InvalidOperationException("A Debian package and a Linux desktop are required.");
        var info = createOpener?.Invoke(Path.GetFullPath(path)) ?? new ProcessStartInfo("xdg-open");
        info.UseShellExecute = false;
        info.RedirectStandardError = true;
        info.RedirectStandardOutput = true;
        if (createOpener is null) info.ArgumentList.Add(Path.GetFullPath(path));
        using var process = Process.Start(info) ?? throw new IOException("System package installer could not be started.");
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        // xdg-open kann bis zum Schließen des Installationsfensters laufen. Die Anwendung bleibt bedienbar.
        await process.WaitForExitAsync(cancellationToken);
        await output;
        if (process.ExitCode != 0) throw new IOException("System package installer: " + await error);
    }

    internal static WindowsUpdatePlan PrepareWindows(string archive, Version version)
    {
        if (!OperatingSystem.IsWindows() || Path.GetFileName(Environment.ProcessPath) != "LIS2.App.Avalonia.exe")
            throw new InvalidOperationException("A portable Windows installation is required.");
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        var parent = Directory.GetParent(target)?.FullName ?? throw new IOException("Invalid installation directory.");
        var session = Path.GetDirectoryName(Path.GetFullPath(archive))!;
        var identifier = Guid.NewGuid().ToString("N");
        var stage = Path.Combine(parent, ".lis2-update-" + identifier);
        var backup = Path.Combine(parent, ".lis2-previous-" + identifier);
        Directory.CreateDirectory(stage); // Prüft Schreibrechte vor dem Beenden der Anwendung.
        CopyDirectory(target, stage);
        ExtractWindowsPackage(archive, stage, version);
        var helper = Path.Combine(session, "helper");
        Directory.CreateDirectory(helper);
        CopyDirectory(target, helper);
        using var current = Process.GetCurrentProcess();
        var plan = new WindowsUpdatePlan(target, stage, backup, session, current.Id, current.StartTime.ToUniversalTime().Ticks);
        File.WriteAllText(Path.Combine(session, "install.json"), JsonSerializer.Serialize(plan));
        return plan;
    }

    /// <summary>Archive dürfen weder außerhalb des Zielordners schreiben noch Verknüpfungen einschleusen.</summary>
    internal static void ExtractWindowsPackage(string archivePath, string destination, Version version)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        long expanded = 0;
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.StartsWith('/') || name.Split('/').Any(part => part is ".." or "." || part.Contains(':'))
                || (entry.ExternalAttributes >> 16 & 0xf000) == 0xa000 || (entry.ExternalAttributes & 0x400) != 0)
                throw new InvalidDataException("Unsafe update archive entry.");
            var path = Path.GetFullPath(Path.Combine(destination, name));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !names.Add(path))
                throw new InvalidDataException("Unsafe or duplicate update archive entry.");
            expanded += entry.Length;
            if (expanded > 1024L * 1024 * 1024) throw new InvalidDataException("Expanded update package too large.");
            if (name.EndsWith('/')) { Directory.CreateDirectory(path); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path, true);
        }
        // Eine alte kopierte Datei darf keine fehlende Datei des neuen Pakets ersetzen.
        foreach (var required in new[] { "LIS2.App.Avalonia.exe", "LIS2.App.Avalonia.dll", "LIS2.App.Avalonia.deps.json", "LIS2.App.Avalonia.runtimeconfig.json" })
            if (!names.Contains(Path.Combine(destination, required))) throw new InvalidDataException("Incomplete Windows update package.");
        using var deps = JsonDocument.Parse(File.ReadAllText(Path.Combine(destination, "LIS2.App.Avalonia.deps.json")));
        if (!deps.RootElement.GetProperty("libraries").TryGetProperty("LIS2.App.Avalonia/" + version, out _))
            throw new InvalidDataException("Update package version mismatch.");
        using var executable = File.OpenRead(Path.Combine(destination, "LIS2.App.Avalonia.exe"));
        using var reader = new BinaryReader(executable);
        if (executable.Length < 88 || reader.ReadUInt16() != 0x5a4d) throw new InvalidDataException("Invalid Windows update executable.");
        executable.Position = 60;
        var header = reader.ReadUInt32();
        if (header > executable.Length - 24) throw new InvalidDataException("Invalid Windows update executable.");
        executable.Position = header;
        if (reader.ReadUInt32() != 0x00004550 || reader.ReadUInt16() != 0x8664)
            throw new InvalidDataException("Windows update executable is not x64.");
    }

    internal static void CopyDirectory(string source, string destination)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(source))
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Installation contains links; automatic replacement is not supported.");
            var target = Path.Combine(destination, Path.GetFileName(path));
            if (Directory.Exists(path)) { Directory.CreateDirectory(target); CopyDirectory(path, target); }
            else File.Copy(path, target, true);
        }
    }

    internal static Process StartWindowsHelper(WindowsUpdatePlan plan)
    {
        var info = new ProcessStartInfo(Path.Combine(plan.Session, "helper", "LIS2.App.Avalonia.exe")) { UseShellExecute = false, WorkingDirectory = plan.Session };
        info.ArgumentList.Add("--apply-update");
        info.ArgumentList.Add(Path.Combine(plan.Session, "install.json"));
        return Process.Start(info) ?? throw new IOException("Update process could not be started.");
    }

    /// <summary>Jeder fehlgeschlagene Neustart stellt den gesicherten Installationsordner wieder her.</summary>
    internal static async Task ReplaceAsync(WindowsUpdatePlan plan, Func<string, Task<bool>> restart)
    {
        var moved = false;
        var installed = false;
        try
        {
            Directory.Move(plan.Target, plan.Backup);
            moved = true;
            Directory.Move(plan.Stage, plan.Target);
            installed = true;
            if (!await restart(plan.Target)) throw new IOException("Updated application did not confirm startup.");
            await File.WriteAllTextAsync(Path.Combine(plan.Session, "result.txt"), "OK\n" + plan.Backup);
        }
        catch (Exception error)
        {
            if (installed) Directory.Move(plan.Target, plan.Stage);
            if (moved) Directory.Move(plan.Backup, plan.Target);
            await File.WriteAllTextAsync(Path.Combine(plan.Session, "result.txt"), "ERROR\n" + error.Message);
            throw;
        }
    }

    internal static async Task<int> RunWindowsHelperAsync(string planPath)
    {
        WindowsUpdatePlan? plan = null;
        var validated = false;
        try
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            plan = JsonSerializer.Deserialize<WindowsUpdatePlan>(await File.ReadAllTextAsync(planPath)) ?? throw new InvalidDataException("Invalid update plan.");
            ValidatePlan(plan, planPath);
            validated = true;
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            try
            {
                using var previous = Process.GetProcessById(plan.ProcessId);
                if (previous.StartTime.ToUniversalTime().Ticks != plan.ProcessStartTicks) throw new IOException("Update process identity mismatch.");
                await previous.WaitForExitAsync(timeout.Token);
            }
            catch (ArgumentException) { /* Der ursprüngliche Prozess ist bereits beendet. */ }
            if (!File.Exists(Path.Combine(plan.Session, "armed"))) throw new IOException("Application shutdown was not confirmed.");
            await ReplaceAsync(plan, async directory =>
            {
                var info = new ProcessStartInfo(Path.Combine(directory, "LIS2.App.Avalonia.exe")) { UseShellExecute = false, WorkingDirectory = directory };
                info.ArgumentList.Add("--update-ready");
                info.ArgumentList.Add(Path.Combine(plan.Session, "ready"));
                if (plan.DesktopSmoke)
                {
                    info.ArgumentList.Add("--desktop-smoke");
                    info.ArgumentList.Add(Path.Combine(plan.Session, "desktop-smoke"));
                }
                using var next = Process.Start(info) ?? throw new IOException("Application restart failed.");
                var healthy = false;
                try
                {
                    for (var attempt = 0; attempt < 600; attempt++)
                    {
                        if (next.HasExited) return false;
                        if (File.Exists(Path.Combine(plan.Session, "ready"))) { healthy = true; return true; }
                        await Task.Delay(100);
                    }
                    return false;
                }
                finally
                {
                    if (!healthy && !next.HasExited) { next.Kill(true); await next.WaitForExitAsync(); }
                }
            });
            return 0;
        }
        catch (Exception error)
        {
            FailureLog.Write("Update", error);
            if (validated && plan is not null && File.Exists(Path.Combine(plan.Session, "result.txt")) && Directory.Exists(plan.Target))
            {
                var info = new ProcessStartInfo(Path.Combine(plan.Target, "LIS2.App.Avalonia.exe")) { UseShellExecute = false };
                info.ArgumentList.Add("--update-result");
                info.ArgumentList.Add(Path.Combine(plan.Session, "result.txt"));
                if (plan.DesktopSmoke)
                {
                    info.ArgumentList.Add("--desktop-smoke");
                    info.ArgumentList.Add(Path.Combine(plan.Session, "rollback-smoke"));
                }
                Process.Start(info)?.Dispose();
            }
            return 1;
        }
    }

    private static void ValidatePlan(WindowsUpdatePlan plan, string path)
    {
        var session = Path.GetDirectoryName(Path.GetFullPath(path))!;
        if (session != Path.GetFullPath(plan.Session) || Directory.GetParent(session)?.FullName != Path.GetFullPath(ApplicationUpdates.Root)
            || !Guid.TryParseExact(Path.GetFileName(session), "N", out _)
            || Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) != Path.Combine(session, "helper"))
            throw new InvalidDataException("Invalid update session.");
        var parent = Directory.GetParent(plan.Target)?.FullName;
        if (parent is null || Directory.GetParent(plan.Stage)?.FullName != parent
            || Directory.GetParent(plan.Backup)?.FullName != parent || !Path.GetFileName(plan.Stage).StartsWith(".lis2-update-", StringComparison.Ordinal)
            || !Path.GetFileName(plan.Backup).StartsWith(".lis2-previous-", StringComparison.Ordinal))
            throw new InvalidDataException("Invalid update directories.");
    }
}

internal sealed record WindowsUpdatePlan(string Target, string Stage, string Backup, string Session, int ProcessId, long ProcessStartTicks, bool DesktopSmoke = false);
