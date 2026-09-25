# Home Assistant integration

LIS2 Control Center can use Home Assistant as a live display data source.

## Transport

The integration uses Home Assistant's WebSocket API at `/api/websocket`.

Connection flow:

1. connect with `ws://` or `wss://` derived from the configured Home Assistant URL,
2. wait for `auth_required`,
3. authenticate with a long-lived access token,
4. request the initial state dump with `get_states`,
5. subscribe to `state_changed`,
6. update the LIS2 data-source snapshot as events arrive,
7. reconnect automatically after a lost connection.

Official API documentation:

- https://developers.home-assistant.io/docs/api/websocket/
- https://developers.home-assistant.io/docs/auth_api/

## Configuration

The Home Assistant page accepts:

- Home Assistant base URL, for example `http://homeassistant.local:8123`
- a Home Assistant long-lived access token
- an enable/disable switch

The **Test connection** action authenticates a temporary WebSocket connection without changing the saved running configuration.

**Save / reconnect** stores the configuration and restarts the Home Assistant source.

## Display variables

Every current Home Assistant entity is exposed as:

```text
{HA.sensor.example_temperature}
{HA.binary_sensor.example_window}
{HA.switch.example}
```

The browser shows:

- entity ID
- friendly name
- domain
- current state
- unit of measurement
- availability
- template key

A user-defined alias creates an additional stable template name. For example, an alias named `DuemmerOutside` exposes:

```text
{HA.DuemmerOutside}
```

The original entity-ID based variable remains available.

## Updates

This is not a polling integration. After the initial `get_states` snapshot, changes arrive from Home Assistant through `state_changed` WebSocket events.

The source publishes an immutable snapshot to the existing `DataSourceRegistry`. Display rendering is triggered through the application's coalescing render path, so bursts of Home Assistant events do not create overlapping LIS2 writes.

## Scope

The initial integration is deliberately read-only.

It does not call Home Assistant services, change entity state, or expose LIS2 hardware control to Home Assistant. This keeps the Home Assistant source aligned with the existing source abstraction: it supplies values for pages and display logic.
