namespace LIS2.App.Avalonia;

/// <summary>Bestätigt den tatsächlich geöffneten Desktop gegenüber dem externen Updateprozess.</summary>
internal static class UpdateStartup
{
    internal static string? ReadyFile { get; private set; }
    internal static string? ResultFile { get; private set; }

    internal static void Configure(string[] arguments)
    {
        ReadyFile = ReadPath(arguments, "--update-ready", "ready");
        ResultFile = ReadPath(arguments, "--update-result", "result.txt");
    }

    private static string? ReadPath(string[] arguments, string option, string filename)
    {
        var index = Array.IndexOf(arguments, option);
        if (index < 0) return null;
        if (index + 1 >= arguments.Length) throw new InvalidDataException("Missing update session.");
        var path = Path.GetFullPath(arguments[index + 1]);
        var directory = Path.GetDirectoryName(path)!;
        if (Path.GetFileName(path) != filename || Directory.GetParent(directory)?.FullName != Path.GetFullPath(ApplicationUpdates.Root)
            || !Guid.TryParseExact(Path.GetFileName(directory), "N", out _))
            throw new InvalidDataException("Invalid update session.");
        return path;
    }

    internal static void NotifyReady()
    {
        if (ReadyFile is not null) File.WriteAllText(ReadyFile, ApplicationUpdates.InstalledVersion.ToString());
    }
}
