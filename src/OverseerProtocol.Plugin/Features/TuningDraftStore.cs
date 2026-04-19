using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using OverseerProtocol.Configuration;

namespace OverseerProtocol.Features;

internal sealed class TuningDraftStore
{
    public LobbySettingsDraft Lobby { get; } = new();
    public List<MoonDraft> Moons { get; } = new();
    public List<ItemDraft> Items { get; } = new();
    public List<SpawnDraft> Spawns { get; } = new();
    public List<string> EnemyIds { get; } = new();

    public static TuningDraftStore Load()
    {
        var provider = new RuntimeCatalogMenuProvider();
        var store = new TuningDraftStore();
        store.LoadLobby();
        store.EnemyIds.AddRange(provider.LoadEnemyIds());

        foreach (var moon in provider.LoadMoons())
            store.Moons.Add(MoonDraft.FromConfig(moon));

        foreach (var item in provider.LoadItems())
            store.Items.Add(ItemDraft.FromConfig(item));

        foreach (var spawn in provider.LoadSpawns())
            store.Spawns.Add(SpawnDraft.FromConfig(spawn));

        return store;
    }

    public void ApplyPreset(LobbyPresetDefinition preset)
    {
        Lobby.PresetId = preset.Id;
        Lobby.EnableMultiplayer = preset.EnableMultiplayer;
        Lobby.MaxPlayers = preset.MaxPlayers;
        Lobby.EnableLateJoin = preset.EnableLateJoin;
        Lobby.LateJoinInOrbit = preset.LateJoinInOrbit;
        Lobby.LateJoinOnMoonAsSpectator = preset.LateJoinOnMoonAsSpectator;
        Lobby.RequireSameModVersion = preset.RequireSameModVersion;
        Lobby.RequireSameConfigHash = preset.RequireSameConfigHash;
    }

    public void Save()
    {
        OPConfig.ActivePreset.Value = Lobby.PresetId;
        OPConfig.LastLobbyPreset.Value = Lobby.PresetId;
        OPConfig.EnableMultiplayer.Value = Lobby.EnableMultiplayer;
        OPConfig.MaxPlayers.Value = Math.Max(1, Math.Min(64, Lobby.MaxPlayers));
        OPConfig.EnableLateJoin.Value = Lobby.EnableLateJoin;
        OPConfig.LateJoinInOrbit.Value = Lobby.LateJoinInOrbit;
        OPConfig.LateJoinOnMoonAsSpectator.Value = Lobby.LateJoinOnMoonAsSpectator;
        OPConfig.RequireSameModVersion.Value = Lobby.RequireSameModVersion;
        OPConfig.RequireSameConfigHash.Value = Lobby.RequireSameConfigHash;

        foreach (var moon in Moons)
            moon.Save();

        foreach (var item in Items)
            item.Save();

        foreach (var spawn in Spawns)
            spawn.Save();

        OPConfig.ConfigFile.Save();
    }

    private void LoadLobby()
    {
        var preset = LobbyPresetDefinition.Resolve(OPConfig.LastLobbyPreset?.Value ?? OPConfig.ActivePresetName);
        Lobby.PresetId = preset.Id;
        Lobby.EnableMultiplayer = OPConfig.EnableMultiplayer.Value;
        Lobby.MaxPlayers = OPConfig.MaxPlayers.Value;
        Lobby.EnableLateJoin = OPConfig.EnableLateJoin.Value;
        Lobby.LateJoinInOrbit = OPConfig.LateJoinInOrbit.Value;
        Lobby.LateJoinOnMoonAsSpectator = OPConfig.LateJoinOnMoonAsSpectator.Value;
        Lobby.RequireSameModVersion = OPConfig.RequireSameModVersion.Value;
        Lobby.RequireSameConfigHash = OPConfig.RequireSameConfigHash.Value;
    }

    internal static ConfigEntry<bool> BindBool(string section, string key, bool value, string description) =>
        OPConfig.ConfigFile.Bind(section, key, value, description);

    internal static ConfigEntry<string> BindString(string section, string key, string value, string description) =>
        OPConfig.ConfigFile.Bind(section, key, value ?? "", description);

    internal static ConfigEntry<int> BindInt(string section, string key, int value, string description, int min, int max) =>
        OPConfig.ConfigFile.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<int>(min, max)));

    internal static ConfigEntry<float> BindFloat(string section, string key, float value, string description, float min, float max) =>
        OPConfig.ConfigFile.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
}

internal sealed class LobbySettingsDraft
{
    public string PresetId { get; set; } = OPConfig.DefaultPreset;
    public bool EnableMultiplayer { get; set; }
    public int MaxPlayers { get; set; } = 4;
    public bool EnableLateJoin { get; set; }
    public bool LateJoinInOrbit { get; set; } = true;
    public bool LateJoinOnMoonAsSpectator { get; set; }
    public bool RequireSameModVersion { get; set; } = true;
    public bool RequireSameConfigHash { get; set; }
}

internal sealed class MoonDraft
{
    public MenuMoonRecord Baseline { get; private set; } = new();
    public bool Enabled { get; set; }
    public int RoutePrice { get; set; }
    public int RiskLevel { get; set; }
    public string RiskLabel { get; set; } = "";
    public string Description { get; set; } = "";
    public int MinScrap { get; set; }
    public int MaxScrap { get; set; }
    public int MinTotalScrapValue { get; set; }
    public int MaxTotalScrapValue { get; set; }

    public static MoonDraft FromConfig(MenuMoonRecord moon)
    {
        var section = "Moons." + moon.Id;
        return new MoonDraft
        {
            Baseline = moon,
            Enabled = TuningDraftStore.BindBool(section, "Enabled", false, "Enable edits for this moon.").Value,
            RoutePrice = TuningDraftStore.BindInt(section, "RoutePrice", moon.RoutePrice, "Terminal route price.", 0, 99999).Value,
            RiskLabel = TuningDraftStore.BindString(section, "Tier", string.IsNullOrWhiteSpace(moon.RiskLabel) ? "None" : moon.RiskLabel, "Risk label.").Value,
            RiskLevel = TuningDraftStore.BindInt(section, "RiskLevel", moon.RiskLevel, "Numeric risk level.", 0, 5).Value,
            Description = TuningDraftStore.BindString(section, "Description", moon.Description, "Moon description text.").Value,
            MinScrap = TuningDraftStore.BindInt(section, "MinScrap", moon.MinScrap, "Minimum scrap items.", 0, 999).Value,
            MaxScrap = TuningDraftStore.BindInt(section, "MaxScrap", moon.MaxScrap, "Maximum scrap items.", 0, 999).Value,
            MinTotalScrapValue = TuningDraftStore.BindInt(section, "MinTotalScrapValue", moon.MinTotalScrapValue, "Minimum total scrap value.", 0, 99999).Value,
            MaxTotalScrapValue = TuningDraftStore.BindInt(section, "MaxTotalScrapValue", moon.MaxTotalScrapValue, "Maximum total scrap value.", 0, 99999).Value
        };
    }

    public void Reset()
    {
        Enabled = false;
        RoutePrice = Baseline.RoutePrice;
        RiskLevel = Baseline.RiskLevel;
        RiskLabel = string.IsNullOrWhiteSpace(Baseline.RiskLabel) ? "None" : Baseline.RiskLabel;
        Description = Baseline.Description;
        MinScrap = Baseline.MinScrap;
        MaxScrap = Baseline.MaxScrap;
        MinTotalScrapValue = Baseline.MinTotalScrapValue;
        MaxTotalScrapValue = Baseline.MaxTotalScrapValue;
    }

    public void Save()
    {
        var section = "Moons." + Baseline.Id;
        TuningDraftStore.BindBool(section, "Enabled", false, "Enable edits for this moon.").Value = Enabled;
        TuningDraftStore.BindString(section, "DisplayName", Baseline.DisplayName, "Observed moon display name. Informational.").Value = Baseline.DisplayName;
        TuningDraftStore.BindInt(section, "RoutePrice", Baseline.RoutePrice, "Terminal route price.", 0, 99999).Value = RoutePrice;
        TuningDraftStore.BindString(section, "Tier", Baseline.RiskLabel, "Risk label.").Value = RiskLabel;
        TuningDraftStore.BindInt(section, "RiskLevel", Baseline.RiskLevel, "Numeric risk level.", 0, 5).Value = RiskLevel;
        TuningDraftStore.BindString(section, "Description", Baseline.Description, "Moon description text.").Value = Description;
        TuningDraftStore.BindInt(section, "MinScrap", Baseline.MinScrap, "Minimum scrap items.", 0, 999).Value = MinScrap;
        TuningDraftStore.BindInt(section, "MaxScrap", Baseline.MaxScrap, "Maximum scrap items.", 0, 999).Value = MaxScrap;
        TuningDraftStore.BindInt(section, "MinTotalScrapValue", Baseline.MinTotalScrapValue, "Minimum total scrap value.", 0, 99999).Value = MinTotalScrapValue;
        TuningDraftStore.BindInt(section, "MaxTotalScrapValue", Baseline.MaxTotalScrapValue, "Maximum total scrap value.", 0, 99999).Value = MaxTotalScrapValue;
    }
}

internal sealed class ItemDraft
{
    public MenuItemRecord Baseline { get; private set; } = new();
    public bool Enabled { get; set; }
    public int Value { get; set; }
    public float Weight { get; set; }
    public bool IsScrap { get; set; }
    public int MinScrapValue { get; set; }
    public int MaxScrapValue { get; set; }
    public bool RequiresBattery { get; set; }
    public bool Conductive { get; set; }
    public bool TwoHanded { get; set; }

    public static ItemDraft FromConfig(MenuItemRecord item)
    {
        var section = "Items." + item.Id;
        return new ItemDraft
        {
            Baseline = item,
            Enabled = TuningDraftStore.BindBool(section, "Enabled", false, "Enable edits for this item.").Value,
            Value = TuningDraftStore.BindInt(section, "Value", item.Value, "Item value/store price.", 0, 99999).Value,
            Weight = TuningDraftStore.BindFloat(section, "Weight", item.Weight, "Item weight multiplier.", 0f, 100f).Value,
            IsScrap = TuningDraftStore.BindBool(section, "IsScrap", item.IsScrap, "Whether the item is scrap.").Value,
            MinScrapValue = TuningDraftStore.BindInt(section, "MinScrapValue", item.MinScrapValue, "Minimum scrap value.", 0, 99999).Value,
            MaxScrapValue = TuningDraftStore.BindInt(section, "MaxScrapValue", item.MaxScrapValue, "Maximum scrap value.", 0, 99999).Value,
            RequiresBattery = TuningDraftStore.BindBool(section, "RequiresBattery", item.RequiresBattery, "Whether the item uses battery.").Value,
            Conductive = TuningDraftStore.BindBool(section, "Conductive", item.Conductive, "Whether this item conducts lightning.").Value,
            TwoHanded = TuningDraftStore.BindBool(section, "TwoHanded", item.TwoHanded, "Whether this item uses both hands.").Value
        };
    }

    public void Reset()
    {
        Enabled = false;
        Value = Baseline.Value;
        Weight = Baseline.Weight;
        IsScrap = Baseline.IsScrap;
        MinScrapValue = Baseline.MinScrapValue;
        MaxScrapValue = Baseline.MaxScrapValue;
        RequiresBattery = Baseline.RequiresBattery;
        Conductive = Baseline.Conductive;
        TwoHanded = Baseline.TwoHanded;
    }

    public void Save()
    {
        var section = "Items." + Baseline.Id;
        TuningDraftStore.BindBool(section, "Enabled", false, "Enable edits for this item.").Value = Enabled;
        TuningDraftStore.BindString(section, "DisplayName", Baseline.DisplayName, "Observed display name. Informational.").Value = Baseline.DisplayName;
        TuningDraftStore.BindInt(section, "Value", Baseline.Value, "Item value/store price.", 0, 99999).Value = Value;
        TuningDraftStore.BindFloat(section, "Weight", Baseline.Weight, "Item weight multiplier.", 0f, 100f).Value = Weight;
        TuningDraftStore.BindBool(section, "IsScrap", Baseline.IsScrap, "Whether the item is scrap.").Value = IsScrap;
        TuningDraftStore.BindBool(section, "InStore", false, "Whether this item should be present in the Terminal store.");
        TuningDraftStore.BindInt(section, "StorePrice", Baseline.Value, "Price used when the item is in the Terminal store.", 0, 99999).Value = Value;
        TuningDraftStore.BindInt(section, "MinScrapValue", Baseline.MinScrapValue, "Minimum scrap value.", 0, 99999).Value = MinScrapValue;
        TuningDraftStore.BindInt(section, "MaxScrapValue", Baseline.MaxScrapValue, "Maximum scrap value.", 0, 99999).Value = MaxScrapValue;
        TuningDraftStore.BindBool(section, "RequiresBattery", Baseline.RequiresBattery, "Whether the item uses battery.").Value = RequiresBattery;
        TuningDraftStore.BindBool(section, "Conductive", Baseline.Conductive, "Whether this item conducts lightning.").Value = Conductive;
        TuningDraftStore.BindBool(section, "TwoHanded", Baseline.TwoHanded, "Whether this item uses both hands.").Value = TwoHanded;
    }
}

internal sealed class SpawnDraft
{
    public MenuSpawnRecord Baseline { get; private set; } = new();
    public bool InsideEnabled { get; set; }
    public string InsideEnemies { get; set; } = "";
    public bool OutsideEnabled { get; set; }
    public string OutsideEnemies { get; set; } = "";
    public bool DaytimeEnabled { get; set; }
    public string DaytimeEnemies { get; set; } = "";

    public static SpawnDraft FromConfig(MenuSpawnRecord spawn)
    {
        var section = "Moons." + spawn.MoonId;
        return new SpawnDraft
        {
            Baseline = spawn,
            InsideEnabled = TuningDraftStore.BindBool(section, "InsideEnemiesEnabled", false, "Enable replacement for InsideEnemies.").Value,
            InsideEnemies = TuningDraftStore.BindString(section, "InsideEnemies", spawn.InsideEnemies, "EnemyId:rarity entries.").Value,
            OutsideEnabled = TuningDraftStore.BindBool(section, "OutsideEnemiesEnabled", false, "Enable replacement for OutsideEnemies.").Value,
            OutsideEnemies = TuningDraftStore.BindString(section, "OutsideEnemies", spawn.OutsideEnemies, "EnemyId:rarity entries.").Value,
            DaytimeEnabled = TuningDraftStore.BindBool(section, "DaytimeEnemiesEnabled", false, "Enable replacement for DaytimeEnemies.").Value,
            DaytimeEnemies = TuningDraftStore.BindString(section, "DaytimeEnemies", spawn.DaytimeEnemies, "EnemyId:rarity entries.").Value
        };
    }

    public void Reset()
    {
        InsideEnabled = false;
        OutsideEnabled = false;
        DaytimeEnabled = false;
        InsideEnemies = Baseline.InsideEnemies;
        OutsideEnemies = Baseline.OutsideEnemies;
        DaytimeEnemies = Baseline.DaytimeEnemies;
    }

    public void Save()
    {
        var section = "Moons." + Baseline.MoonId;
        TuningDraftStore.BindBool(section, "InsideEnemiesEnabled", false, "Enable replacement for InsideEnemies.").Value = InsideEnabled;
        TuningDraftStore.BindString(section, "InsideEnemies", Baseline.InsideEnemies, "EnemyId:rarity entries.").Value = InsideEnemies;
        TuningDraftStore.BindBool(section, "OutsideEnemiesEnabled", false, "Enable replacement for OutsideEnemies.").Value = OutsideEnabled;
        TuningDraftStore.BindString(section, "OutsideEnemies", Baseline.OutsideEnemies, "EnemyId:rarity entries.").Value = OutsideEnemies;
        TuningDraftStore.BindBool(section, "DaytimeEnemiesEnabled", false, "Enable replacement for DaytimeEnemies.").Value = DaytimeEnabled;
        TuningDraftStore.BindString(section, "DaytimeEnemies", Baseline.DaytimeEnemies, "EnemyId:rarity entries.").Value = DaytimeEnemies;
    }

    public static bool LooksLikeSpawnList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var entries = value.Split(',');
        foreach (var entry in entries)
        {
            var trimmed = entry.Trim();
            if (trimmed.Length == 0)
                continue;

            var separator = trimmed.LastIndexOf(':');
            if (separator <= 0 || separator >= trimmed.Length - 1)
                return false;

            if (!int.TryParse(trimmed.Substring(separator + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return false;
        }

        return true;
    }
}
