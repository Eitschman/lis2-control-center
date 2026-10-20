# Home Assistant integration

LIS2 Control Center can use Home Assistant as a live display data source.

## Avalonia-Branch

Stand 20.10.2026, PORT-02: Avalonia verwendet die gemeinsame WebSocket-Quelle und bietet eine eigene Home-Assistant-Ansicht mit Entitäten, Attributen, Alias/Favorit, Schlüsseln und Seitenerzeugung. Der Quellen-Snapshot fließt in Seiten und den Variablenbrowser. Automatische Tests prüfen Konvertierung/Schlüssel, Workspace, Aktivierung mit vorhandenem Testtoken und den gemeinsamen Seitenaktionsablauf, keine aktuelle echte HA-Verbindung. Nachweise stehen im [Status](prp/linux-port-status.md). Eine Benutzerbestätigung betrifft Windows-Konnektivität; Linux-Liveprüfung bleibt offen.

URL, Aktivierung und Präferenzen werden im Workspace gespeichert. Explizites Deaktivieren bleibt auch bei vorhandenem Token erhalten. Workspaces ohne Aktivierungsfeld verwenden weiterhin die tokenbasierte Aktivierung. Tokens stehen außerhalb des Workspace: `PortableHaTokenStore` verwendet DPAPI auf Windows und `secret-tool` auf Linux. Bei fehlendem Secret Service schreibt der Linux-Code automatisch AES-GCM-Chiffretext und einen lokalen Schlüssel mit eingeschränkten Unix-Rechten. Die Dateien schützen nicht gegen Zugriff mit demselben Benutzerkonto. Dieser Ausweichpfad ist als offene Abweichung von der Freigabepolitik erfasst, keine nachgewiesene Gleichwertigkeit zum nativen Store. Der Windows-Importer übernimmt Aktivierung, URL und Präferenzen, keine Windows-Tokens. Er beendet die bestehende HA-Verbindung; bei Bedarf anschließend neu verbinden.

`CreateHaPageAsync` verwendet für Entitäts- und Attributseiten den gemeinsamen Ablauf `PageWorkspaceActions`: aktuellen Editorentwurf validieren und übernehmen, Seite anlegen, laufende virtuelle und serielle Sequenzen aktualisieren und speichern. Ungültige Entwürfe verhindern den Wechsel. Live-Deaktivierung und Wiederverbindung bleiben in [TODO](../TODO.md) offen. Die nachstehenden Protokoll- und Schlüsselbeschreibungen gelten weiterhin; die Konfigurationsaktionen sind gegen die jeweilige Oberfläche zu prüfen.

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

## Entity attributes

Home Assistant entity attributes are exposed as additional template values:

```text
{HA.sensor.example.attribute.device_class}
{HA.sensor.example.attribute.friendly_name}
{HA.sensor.example.attribute.some_integration_specific_value}
```

When an entity has an alias, the same attributes are also available through the alias:

```text
{HA.ServerDisk.attribute.free}
{HA.ServerDisk.attribute.total}
```

The Home Assistant page lists all attributes of the selected entity with their current value and template key. A selected attribute can be turned directly into a display page.

Scalar JSON values retain their natural string/number/boolean representation. Structured array/object attributes are retained as compact JSON text rather than being discarded.

## Updates

This is not a polling integration. After the initial `get_states` snapshot, changes arrive from Home Assistant through `state_changed` WebSocket events.

The source publishes an immutable snapshot to the existing `DataSourceRegistry`. Display rendering is triggered through the application's coalescing render path, so bursts of Home Assistant events do not create overlapping LIS2 writes.

## Scope

The initial integration is deliberately read-only.

It does not call Home Assistant services, change entity state, or expose LIS2 hardware control to Home Assistant. This keeps the Home Assistant source aligned with the existing source abstraction: it supplies values for pages and display logic.
