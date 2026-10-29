using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LIS2.App.Avalonia;

/// <summary>Öffentliche GitHub-Releases sind die Vertrauensquelle; Pakete werden vor Übergabe geprüft.</summary>
internal sealed class ApplicationUpdates(HttpClient client)
{
    internal const string Repository = "https://github.com/Eitschman/lis2-control-center";
    internal const string Endpoint = "https://api.github.com/repos/Eitschman/lis2-control-center/releases/latest";
    internal static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LIS2ControlCenter", "updates");
    internal static Version InstalledVersion => ParseVersion(typeof(App).Assembly.GetName().Version!.ToString(3));
    internal static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LIS2-Control-Center/" + InstalledVersion);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    internal static Version ParseVersion(string value)
    {
        if (!Regex.IsMatch(value, @"^v?\d+\.\d+\.\d+$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Invalid release version.");
        return Version.Parse(value.TrimStart('v'));
    }

    internal static string AssetName(Version version, bool windows, System.Runtime.InteropServices.Architecture architecture, bool debian)
    {
        var cpu = architecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 when !windows => "arm64",
            _ => throw new PlatformNotSupportedException("Unsupported update architecture.")
        };
        return windows ? $"LIS2-Control-Center-{version}-win-x64.zip" : debian
            ? $"lis2-control-center_{version}_{(cpu == "x64" ? "amd64" : "arm64")}.deb"
            : $"LIS2-Control-Center-{version}-linux-{cpu}.tar.gz";
    }

    internal async Task<AvailableUpdate> CheckAsync(bool windows, System.Runtime.InteropServices.Architecture architecture,
        bool debian, CancellationToken cancellationToken)
    {
        var content = await ReadAsync(new Uri(Endpoint), 2 * 1024 * 1024, cancellationToken);
        using var document = JsonDocument.Parse(content);
        var release = document.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException("Release is not stable.");
        var tag = release.GetProperty("tag_name").GetString()!;
        var version = ParseVersion(tag);
        if (tag != "v" + version) throw new InvalidDataException("Invalid release tag.");
        var name = AssetName(version, windows, architecture, debian);
        var assets = release.GetProperty("assets").EnumerateArray().ToArray();
        Uri Asset(string assetName)
        {
            var matches = assets.Where(asset => asset.GetProperty("name").GetString() == assetName).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("Release asset missing or ambiguous: " + assetName);
            var url = matches[0].GetProperty("browser_download_url").GetString();
            var expected = $"{Repository}/releases/download/{tag}/{assetName}";
            if (!string.Equals(url, expected, StringComparison.Ordinal)) throw new InvalidDataException("Untrusted release asset URL.");
            return new Uri(expected);
        }
        return new(version, name, release.GetProperty("body").GetString() ?? "", Asset(name), Asset("SHA256SUMS.txt"));
    }

    internal async Task<VerifiedUpdate> DownloadAsync(AvailableUpdate update, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (update.Version <= InstalledVersion) throw new InvalidOperationException("No newer release available.");
        // Auch von internen Aufrufern dürfen keine anderen Hosts oder Dateinamen eingeschleust werden.
        var prefix = $"{Repository}/releases/download/v{update.Version}/";
        if (update.Package.AbsoluteUri != prefix + update.AssetName || update.Checksums.AbsoluteUri != prefix + "SHA256SUMS.txt"
            || Path.GetFileName(update.AssetName) != update.AssetName || update.AssetName.Contains('\\'))
            throw new InvalidDataException("Untrusted release asset URL.");
        var checksums = System.Text.Encoding.UTF8.GetString(await ReadAsync(update.Checksums, 64 * 1024, cancellationToken));
        var digest = ExpectedChecksum(checksums, update.AssetName);
        var directory = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, update.AssetName);
        try
        {
            using var response = await client.GetAsync(update.Package, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            if (total is > 300 * 1024 * 1024) throw new InvalidDataException("Update package too large.");
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
            {
                var buffer = new byte[65536];
                long received = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    received += count;
                    if (received > 300 * 1024 * 1024) throw new InvalidDataException("Update package too large.");
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    if (total is > 0) progress?.Report(100.0 * received / total.Value);
                }
                if (total.HasValue && received != total.Value) throw new InvalidDataException("Incomplete update package.");
            }
            await VerifyAsync(destination, digest, cancellationToken);
            return new(destination, digest);
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            throw;
        }
    }

    internal static string ExpectedChecksum(string checksums, string name)
    {
        var matches = checksums.Split('\n').Select(line => Regex.Match(line.TrimEnd('\r'), @"^([a-fA-F0-9]{64})  (.+)$"))
            .Where(match => match.Success && match.Groups[2].Value == name).ToArray();
        if (matches.Length != 1) throw new InvalidDataException("Checksum missing or ambiguous.");
        return matches[0].Groups[1].Value;
    }

    internal static async Task VerifyAsync(string path, string expected, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var actual = await SHA256.HashDataAsync(stream, cancellationToken);
        if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(expected)))
            throw new InvalidDataException("SHA-256 verification failed.");
    }

    private async Task<byte[]> ReadAsync(Uri url, int limit, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (output.Length + count > limit) throw new InvalidDataException("Update response too large.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }
}

internal sealed record AvailableUpdate(Version Version, string AssetName, string Notes, Uri Package, Uri Checksums);
internal sealed record VerifiedUpdate(string Path, string Sha256);
