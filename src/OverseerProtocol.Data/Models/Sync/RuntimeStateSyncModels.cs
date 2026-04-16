using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Sync;

public sealed class RuntimeStateSyncSnapshotDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public string SnapshotId { get; set; } = "";
    public string CreatedUtc { get; set; } = "";
    public string ActivePreset { get; set; } = "default";
    public List<SyncStateEntry> ShipState { get; set; } = new();
    public List<SyncStateEntry> MoonState { get; set; } = new();
    public List<SyncStateEntry> ObjectState { get; set; } = new();
}

public sealed class SyncStateEntry
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string State { get; set; } = "";
    public string Source { get; set; } = "Reserved";
}
