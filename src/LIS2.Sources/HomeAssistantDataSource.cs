using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LIS2.Sources;

public sealed class HomeAssistantDataSource : IDataSource
{
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private IReadOnlyDictionary<string, object?> _values =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<HomeAssistantEntityInfo> _entities =
        Array.Empty<HomeAssistantEntityInfo>();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private ClientWebSocket? _socket;

    private string _baseUrl = string.Empty;
    private string _accessToken = string.Empty;

    public string Id => "HA";

    public IReadOnlyDictionary<string, object?> Values =>
        Volatile.Read(ref _values);

    public IReadOnlyList<HomeAssistantEntityInfo> Entities =>
        Volatile.Read(ref _entities);

    public bool IsConfigured =>
        Uri.TryCreate(_baseUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(_accessToken);

    public bool IsConnected { get; private set; }

    public string? HomeAssistantVersion { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? LastUpdateAt { get; private set; }

    public event EventHandler? Changed;

    public void Configure(string? baseUrl, string? accessToken)
    {
        _baseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        _accessToken = accessToken?.Trim() ?? string.Empty;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_loopTask is not null)
                return;

            if (!IsConfigured)
            {
                IsConnected = false;
                LastError = null;
                PublishEmpty();
                return;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = RunReconnectLoopAsync(_cts.Token);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        await StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cts is null)
                return;

            await _cts.CancelAsync().ConfigureAwait(false);

            if (_socket is { State: WebSocketState.Open or WebSocketState.CloseReceived })
            {
                try
                {
                    await _socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "LIS2 Control Center stopping",
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                }
            }

            if (_loopTask is not null)
            {
                try
                {
                    await _loopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            _socket?.Dispose();
            _socket = null;
            _loopTask = null;
            _cts.Dispose();
            _cts = null;
            IsConnected = false;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Home Assistant URL and access token are required.");

        using var socket = new ClientWebSocket();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        await socket.ConnectAsync(CreateWebSocketUri(), timeout.Token).ConfigureAwait(false);
        var hello = await ReceiveJsonAsync(socket, timeout.Token).ConfigureAwait(false);

        if (GetType(hello) != "auth_required")
            throw new InvalidOperationException("Unexpected Home Assistant WebSocket greeting.");

        await SendJsonAsync(
            socket,
            new { type = "auth", access_token = _accessToken },
            timeout.Token).ConfigureAwait(false);

        var auth = await ReceiveJsonAsync(socket, timeout.Token).ConfigureAwait(false);
        var type = GetType(auth);

        if (type == "auth_invalid")
        {
            var message = auth.RootElement.TryGetProperty("message", out var rawMessage)
                ? rawMessage.GetString()
                : "Authentication failed.";
            throw new InvalidOperationException(message ?? "Authentication failed.");
        }

        if (type != "auth_ok")
            throw new InvalidOperationException("Home Assistant WebSocket authentication failed.");
    }

    private async Task RunReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastError = ex.Message;
                Changed?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _socket?.Dispose();
                _socket = null;
                IsConnected = false;
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ConnectAndReceiveAsync(CancellationToken cancellationToken)
    {
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(CreateWebSocketUri(), cancellationToken).ConfigureAwait(false);

        var greeting = await ReceiveJsonAsync(_socket, cancellationToken).ConfigureAwait(false);
        if (GetType(greeting) != "auth_required")
            throw new InvalidOperationException("Unexpected Home Assistant WebSocket greeting.");

        await SendJsonAsync(
            _socket,
            new { type = "auth", access_token = _accessToken },
            cancellationToken).ConfigureAwait(false);

        var auth = await ReceiveJsonAsync(_socket, cancellationToken).ConfigureAwait(false);
        var authType = GetType(auth);

        if (authType == "auth_invalid")
        {
            var message = auth.RootElement.TryGetProperty("message", out var rawMessage)
                ? rawMessage.GetString()
                : "Authentication failed.";
            throw new InvalidOperationException(message ?? "Authentication failed.");
        }

        if (authType != "auth_ok")
            throw new InvalidOperationException("Home Assistant WebSocket authentication failed.");

        HomeAssistantVersion = auth.RootElement.TryGetProperty("ha_version", out var version)
            ? version.GetString()
            : null;
        LastError = null;
        IsConnected = true;
        Changed?.Invoke(this, EventArgs.Empty);

        await SendJsonAsync(
            _socket,
            new { id = 1, type = "get_states" },
            cancellationToken).ConfigureAwait(false);

        await SendJsonAsync(
            _socket,
            new { id = 2, type = "subscribe_events", event_type = "state_changed" },
            cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested &&
               _socket.State == WebSocketState.Open)
        {
            using var message = await ReceiveJsonAsync(_socket, cancellationToken).ConfigureAwait(false);
            ProcessMessage(message.RootElement);
        }
    }

    private void ProcessMessage(JsonElement message)
    {
        var type = message.TryGetProperty("type", out var rawType)
            ? rawType.GetString()
            : null;

        if (type == "result" &&
            message.TryGetProperty("id", out var rawId) &&
            rawId.GetInt32() == 1 &&
            message.TryGetProperty("success", out var success) &&
            success.GetBoolean() &&
            message.TryGetProperty("result", out var result) &&
            result.ValueKind == JsonValueKind.Array)
        {
            var states = new Dictionary<string, EntityState>(StringComparer.OrdinalIgnoreCase);
            foreach (var state in result.EnumerateArray())
            {
                if (TryReadState(state, out var parsedState))
                    states[parsedState.EntityId] = parsedState;
            }

            Publish(states.Values);
            return;
        }

        if (type != "event" ||
            !message.TryGetProperty("event", out var eventNode) ||
            !eventNode.TryGetProperty("event_type", out var eventType) ||
            eventType.GetString() != "state_changed" ||
            !eventNode.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("entity_id", out var rawEntityId))
        {
            return;
        }

        var entityId = rawEntityId.GetString();
        if (string.IsNullOrWhiteSpace(entityId))
            return;

        var current = Entities.ToDictionary(
            entity => entity.EntityId,
            entity => new EntityState(
                entity.EntityId,
                entity.FriendlyName,
                entity.State,
                entity.Unit,
                entity.Attributes),
            StringComparer.OrdinalIgnoreCase);

        if (data.TryGetProperty("new_state", out var newState) &&
            newState.ValueKind != JsonValueKind.Null &&
            TryReadState(newState, out var parsed))
        {
            current[entityId] = parsed;
        }
        else
        {
            current.Remove(entityId);
        }

        Publish(current.Values);
    }

    private void Publish(IEnumerable<EntityState> states)
    {
        var ordered = states
            .OrderBy(state => state.EntityId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var entities = new List<HomeAssistantEntityInfo>(ordered.Length);

        foreach (var state in ordered)
        {
            var domain = state.EntityId.Split('.', 2)[0];
            var available =
                !string.Equals(state.State, "unavailable", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(state.State, "unknown", StringComparison.OrdinalIgnoreCase);

            values[state.EntityId] = state.State;
            values[$"{state.EntityId}.Unit"] = state.Unit;
            values[$"{state.EntityId}.FriendlyName"] = state.FriendlyName;

            foreach (var attribute in state.Attributes)
                values[$"{state.EntityId}.attribute.{attribute.Key}"] = attribute.Value;

            entities.Add(new HomeAssistantEntityInfo(
                state.EntityId,
                domain,
                state.FriendlyName,
                state.State,
                state.Unit,
                available,
                state.Attributes));
        }

        Volatile.Write(ref _values, values);
        Volatile.Write(ref _entities, entities);
        LastUpdateAt = DateTimeOffset.UtcNow;
        LastError = null;
        IsConnected = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void PublishEmpty()
    {
        Volatile.Write(
            ref _values,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));
        Volatile.Write(
            ref _entities,
            Array.Empty<HomeAssistantEntityInfo>());
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static bool TryReadState(JsonElement state, out EntityState parsed)
    {
        parsed = default!;

        if (!state.TryGetProperty("entity_id", out var rawEntityId) ||
            !state.TryGetProperty("state", out var rawState))
        {
            return false;
        }

        var entityId = rawEntityId.GetString();
        var value = rawState.GetString();

        if (string.IsNullOrWhiteSpace(entityId) || value is null)
            return false;

        var friendlyName = entityId;
        var unit = string.Empty;
        var parsedAttributes =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (state.TryGetProperty("attributes", out var attributes) &&
            attributes.ValueKind == JsonValueKind.Object)
        {
            foreach (var attribute in attributes.EnumerateObject())
                parsedAttributes[attribute.Name] = HomeAssistantValueConverter.Convert(attribute.Value);

            if (attributes.TryGetProperty("friendly_name", out var rawFriendlyName))
                friendlyName = rawFriendlyName.GetString() ?? entityId;

            if (attributes.TryGetProperty("unit_of_measurement", out var rawUnit))
                unit = rawUnit.GetString() ?? string.Empty;
        }

        parsed = new EntityState(
            entityId,
            friendlyName,
            value,
            unit,
            parsedAttributes);
        return true;
    }

    private Uri CreateWebSocketUri()
    {
        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("Home Assistant URL is invalid.");

        var builder = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                ? "wss"
                : "ws",
            Path = baseUri.AbsolutePath.TrimEnd('/') + "/api/websocket",
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri;
    }

    private static async Task SendJsonAsync(
        ClientWebSocket socket,
        object value,
        CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonDocument> ReceiveJsonAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[8192];

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("Home Assistant closed the WebSocket connection.");

            stream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
                break;
        }

        stream.Position = 0;
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static string? GetType(JsonDocument document) =>
        document.RootElement.TryGetProperty("type", out var type)
            ? type.GetString()
            : null;

    private sealed record EntityState(
        string EntityId,
        string FriendlyName,
        string State,
        string Unit,
        IReadOnlyDictionary<string, object?> Attributes);

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _lifecycleLock.Dispose();
    }
}
