# Home Assistant integration

LIS2 Control Center can use Home Assistant as a live display data source.

## Avalonia-Branch

Stand 21.10.2026, PORT-05: Avalonia verwendet die gemeinsame WebSocket-Quelle und bietet eine eigene Home-Assistant-Ansicht mit Entitäten, Attributnamen und Werten, Alias/Favorit, Schlüsseln und Seitenerzeugung. Originalschlüssel und Aliaswerte einschließlich Einheit und Attributen erreichen Seiten und Variablenbrowser. Ein lokaler WebSocket-Protokollpartner prüft Authentifizierung, Anfangszustände, Liveereignisse, Reconnect, fehlerhafte Zustands-/Aboanfragen und Stop ohne kooperierenden Server. Nachweise stehen im [Status](prp/linux-port-status.md). Eine reale HA-Instanz auf den Zielsystemen ist damit nicht geprüft.

URL, Aktivierung, Präferenzen und die ausdrückliche Linux-Auswahl „Token in lokaler AES-Datei speichern“ werden im Workspace gespeichert. Tokens stehen außerhalb des Workspace. Windows verwendet DPAPI; Linux verwendet standardmäßig `secret-tool`, Sitzungs-D-Bus und einen entsperrten Desktop-Schlüsselbund. Fehlt diese Voraussetzung, erscheint ein Fehler. Die App wechselt nicht automatisch zur Dateispeicherung. Erst die ausgewählte lokale Speicherart verwendet AES-GCM mit `ha-token.bin` und `ha-token.key`, beide mit Unix-Dateirechten 0600. Schlüssel und Chiffretext liegen im selben Anwendungsordner; die Dateien schützen nicht gegen Zugriff mit demselben Benutzerkonto. Bei Rückkehr zum nativen Store werden lokale Dateien erst nach erfolgreicher Speicherung und Rückleseprüfung entfernt. Die lokale Auswahl greift nicht auf den Schlüsselbund zu und entfernt dort eventuell vorhandene Einträge nicht. Deaktivieren stoppt HA, speichert die Deaktivierung und löscht das Token aus dem ausgewählten Speicher. Ein Löschfehler bleibt sichtbar, ohne HA erneut zu aktivieren. Workspaces ohne Aktivierungsfeld verwenden weiterhin die tokenbasierte Aktivierung. Der Windows-Importer übernimmt Aktivierung, URL und Präferenzen, keine Windows-Tokens; die lokale Speicherwahl bleibt erhalten.

`CreateHaPageAsync` verwendet für Entitäts- und Attributseiten den gemeinsamen Ablauf `PageWorkspaceActions`: aktuellen Editorentwurf validieren und übernehmen, Seite anlegen, laufende virtuelle und serielle Sequenzen aktualisieren und speichern. Ungültige Entwürfe verhindern den Wechsel. Attributschlüssel verwenden auch mit Alias den Abschnitt `.attribute.`. Verbindungsabbruch und Stop entfernen Zustände und Attribute, statt alte Werte weiter anzuzeigen. Der Reconnect wartet fünf Sekunden; Eingaben akzeptieren HTTP/HTTPS. Die realen Zielsystemprüfungen stehen in [TODO](../TODO.md).

Die Linux-CI installiert `libsecret-tools`, `gnome-keyring` und D-Bus und führt die Tests mit einem privaten Sitzungsbus sowie temporären Schlüsselbund aus. Der künstliche Testschlüsselbund berührt keine Benutzerzugangsdaten. Ohne diese isolierte Umgebung wird der betreffende Integrationstest übersprungen. Fehlerfälle verwenden eigene Programme statt des Desktop-Schlüsselbunds. Protokollreferenz: [Home Assistant WebSocket API](https://contributors.home-assistant.io/core/internals/websocket-api/); Rückgabeverhalten des Werkzeugs: [libsecret secret-tool](https://github.com/GNOME/libsecret/blob/master/tool/secret-tool.c).

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
