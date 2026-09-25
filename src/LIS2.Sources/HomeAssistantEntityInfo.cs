namespace LIS2.Sources;

public sealed record HomeAssistantEntityInfo(
    string EntityId,
    string Domain,
    string FriendlyName,
    string State,
    string Unit,
    bool IsAvailable);
