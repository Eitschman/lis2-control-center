using System.Globalization;

namespace LIS2.App.Avalonia;

internal sealed record LinuxSandboxIdentity(string ApplicationId, string InstanceId);
internal sealed record LinuxSandboxProcess(int HostId, int NamespaceId, LinuxSandboxIdentity Identity);

/// <summary>Ordnet Flatpak-Proxys und interne Prozesskennungen derselben laufenden Sandbox zu.</summary>
internal sealed record LinuxSandboxSelection(LinuxSandboxIdentity Identity, IReadOnlyList<LinuxSandboxProcess> Processes)
{
    internal int? Resolve(int? namespaceId, int? clientHostId, string applicationId, string instanceId)
    {
        // Native PipeWire-Verbindungen tragen die echte Hostkennung; Pulse-Verbindungen die des Audioservers.
        var direct = Processes.SingleOrDefault(process => process.HostId == clientHostId);
        if (direct is not null) return direct.Identity == Identity ? direct.HostId : null;
        if (applicationId != Identity.ApplicationId) return null;
        if (instanceId.Length > 0 && instanceId != Identity.InstanceId) return null;
        if (instanceId.Length == 0 && Processes.Where(process => process.Identity.ApplicationId == applicationId)
                .Select(process => process.Identity.InstanceId).Distinct().Take(2).Count() > 1)
            throw new InvalidOperationException("Cannot distinguish player sandbox instances");
        var matches = Processes.Where(process => process.Identity == Identity && process.NamespaceId == namespaceId).ToArray();
        return matches.Length == 1 ? matches[0].HostId : null;
    }

    internal static LinuxSandboxSelection? Read(int ownerProcessId)
    {
        var identity = ReadIdentity(ownerProcessId);
        if (identity is null) return null;
        var processes = new List<LinuxSandboxProcess>();
        foreach (var directory in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(directory), NumberStyles.None, CultureInfo.InvariantCulture, out var hostId)) continue;
            try
            {
                var namespaceLine = File.ReadLines(Path.Combine(directory, "status"))
                    .FirstOrDefault(line => line.StartsWith("NSpid:", StringComparison.Ordinal));
                var ids = namespaceLine?[6..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (ids is null || ids.Length < 2 || !int.TryParse(ids[^1], out var namespaceId)) continue;
                if (ReadIdentity(hostId) is { } candidate)
                    processes.Add(new(hostId, namespaceId, candidate));
            }
            catch (IOException) { /* Prozesse dürfen während der Bestandsaufnahme enden. */ }
            catch (UnauthorizedAccessException) { /* Fremde Prozesse werden nicht als Quelle verwendet. */ }
        }
        return new(identity, processes);
    }

    private static LinuxSandboxIdentity? ReadIdentity(int processId)
    {
        try { return ParseIdentity(File.ReadAllText("/proc/" + processId.ToString(CultureInfo.InvariantCulture) + "/root/.flatpak-info")); }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    internal static LinuxSandboxIdentity? ParseIdentity(string text)
    {
        var section = "";
        var application = "";
        var instance = "";
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith('[') && line.EndsWith(']')) { section = line[1..^1]; continue; }
            var separator = line.IndexOf('=');
            if (separator < 0) continue;
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (section == "Application" && key == "name") application = value;
            if (section == "Instance" && key == "instance-id") instance = value;
        }
        return application.Length > 0 && instance.Length > 0 ? new(application, instance) : null;
    }
}
