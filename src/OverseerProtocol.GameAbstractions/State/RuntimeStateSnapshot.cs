using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.State;

public sealed class RuntimeStateSnapshot
{
    private readonly Dictionary<string, ItemState> _items = new();
    private readonly Dictionary<string, LevelSpawnState> _levels = new();
    private readonly Dictionary<string, RouteNodeState> _routeNodes = new();

    public bool IsCaptured { get; private set; }

    public void Capture()
    {
        _items.Clear();
        _levels.Clear();
        _routeNodes.Clear();

        var itemCount = CaptureItems();
        var levelCount = CaptureLevels();
        var routeNodeCount = CaptureRouteNodes();

        IsCaptured = true;
        OPLog.Info("Snapshot", $"Captured vanilla runtime state for {itemCount} items, {levelCount} moons, and {routeNodeCount} route nodes.");
    }

    public void Restore()
    {
        if (!IsCaptured)
        {
            OPLog.Warning("Snapshot", "Runtime snapshot has not been captured. Restore skipped.");
            return;
        }

        var itemCount = RestoreItems();
        var levelCount = RestoreLevels();
        var routeNodeCount = RestoreRouteNodes();

        OPLog.Info("Snapshot", $"Restored runtime state for {itemCount} items, {levelCount} moons, and {routeNodeCount} route nodes.");
    }

    private int CaptureItems()
    {
        if (StartOfRound.Instance?.allItemsList?.itemsList == null)
        {
            OPLog.Warning("Snapshot", "Item catalog not available. Item snapshot skipped.");
            return 0;
        }

        foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.name))
                continue;

            _items[item.name] = new ItemState(item.weight, item.creditsWorth);
            OPLog.Info("Snapshot", $"Captured item state: item={item.name}, weight={item.weight:0.###}, creditsWorth={item.creditsWorth}");
        }

        return _items.Count;
    }

    private int CaptureLevels()
    {
        if (StartOfRound.Instance?.levels == null)
        {
            OPLog.Warning("Snapshot", "Moon catalog not available. Spawn snapshot skipped.");
            return 0;
        }

        foreach (var level in StartOfRound.Instance.levels)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.name))
                continue;

            _levels[level.name] = new LevelSpawnState(
                level.riskLevel,
                ClonePool(level.Enemies),
                ClonePool(level.OutsideEnemies),
                ClonePool(level.DaytimeEnemies));
            OPLog.Info(
                "Snapshot",
                $"Captured moon state: moon={level.name}, riskLevel={level.riskLevel}, inside={level.Enemies?.Count ?? 0}, outside={level.OutsideEnemies?.Count ?? 0}, daytime={level.DaytimeEnemies?.Count ?? 0}");
        }

        return _levels.Count;
    }

    private int CaptureRouteNodes()
    {
        var routeNodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        if (routeNodes == null || routeNodes.Length == 0)
            return 0;

        foreach (var node in routeNodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.name))
                continue;

            _routeNodes[node.name] = new RouteNodeState(node.itemCost);
            OPLog.Info("Snapshot", $"Captured route node state: node={node.name}, moonIndex={node.buyRerouteToMoon}, itemCost={node.itemCost}");
        }

        return _routeNodes.Count;
    }

    private int RestoreItems()
    {
        if (StartOfRound.Instance?.allItemsList?.itemsList == null)
        {
            OPLog.Warning("Snapshot", "Item catalog not available. Item restore skipped.");
            return 0;
        }

        var restored = 0;
        foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.name))
                continue;

            if (!_items.TryGetValue(item.name, out var state))
                continue;

            var beforeWeight = item.weight;
            var beforeCredits = item.creditsWorth;
            item.weight = state.Weight;
            item.creditsWorth = state.CreditsWorth;
            OPLog.Info(
                "Snapshot",
                $"Restored item state: item={item.name}, weight {beforeWeight:0.###} -> {item.weight:0.###}, creditsWorth {beforeCredits} -> {item.creditsWorth}");
            restored++;
        }

        return restored;
    }

    private int RestoreLevels()
    {
        if (StartOfRound.Instance?.levels == null)
        {
            OPLog.Warning("Snapshot", "Moon catalog not available. Spawn restore skipped.");
            return 0;
        }

        var restored = 0;
        foreach (var level in StartOfRound.Instance.levels)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.name))
                continue;

            if (!_levels.TryGetValue(level.name, out var state))
                continue;

            var beforeRisk = level.riskLevel;
            var beforeInside = level.Enemies?.Count ?? 0;
            var beforeOutside = level.OutsideEnemies?.Count ?? 0;
            var beforeDaytime = level.DaytimeEnemies?.Count ?? 0;
            level.riskLevel = state.RiskLevel;
            level.Enemies = ClonePool(state.InsideEnemies);
            level.OutsideEnemies = ClonePool(state.OutsideEnemies);
            level.DaytimeEnemies = ClonePool(state.DaytimeEnemies);
            OPLog.Info(
                "Snapshot",
                $"Restored moon state: moon={level.name}, riskLevel {beforeRisk} -> {level.riskLevel}, inside {beforeInside} -> {level.Enemies.Count}, outside {beforeOutside} -> {level.OutsideEnemies.Count}, daytime {beforeDaytime} -> {level.DaytimeEnemies.Count}");
            restored++;
        }

        return restored;
    }

    private int RestoreRouteNodes()
    {
        var routeNodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        if (routeNodes == null || routeNodes.Length == 0)
            return 0;

        var restored = 0;
        foreach (var node in routeNodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.name))
                continue;

            if (!_routeNodes.TryGetValue(node.name, out var state))
                continue;

            var before = node.itemCost;
            node.itemCost = state.ItemCost;
            OPLog.Info("Snapshot", $"Restored route node state: node={node.name}, itemCost {before} -> {node.itemCost}");
            restored++;
        }

        return restored;
    }

    private static List<SpawnableEnemyWithRarity> ClonePool(List<SpawnableEnemyWithRarity>? source)
    {
        var result = new List<SpawnableEnemyWithRarity>();
        if (source == null)
            return result;

        foreach (var entry in source)
        {
            if (entry == null)
                continue;

            result.Add(new SpawnableEnemyWithRarity(entry.enemyType, entry.rarity));
        }

        return result;
    }

    private sealed class ItemState
    {
        public ItemState(float weight, int creditsWorth)
        {
            Weight = weight;
            CreditsWorth = creditsWorth;
        }

        public float Weight { get; }
        public int CreditsWorth { get; }
    }

    private sealed class LevelSpawnState
    {
        public LevelSpawnState(
            string riskLevel,
            List<SpawnableEnemyWithRarity> insideEnemies,
            List<SpawnableEnemyWithRarity> outsideEnemies,
            List<SpawnableEnemyWithRarity> daytimeEnemies)
        {
            RiskLevel = riskLevel;
            InsideEnemies = insideEnemies;
            OutsideEnemies = outsideEnemies;
            DaytimeEnemies = daytimeEnemies;
        }

        public string RiskLevel { get; }
        public List<SpawnableEnemyWithRarity> InsideEnemies { get; }
        public List<SpawnableEnemyWithRarity> OutsideEnemies { get; }
        public List<SpawnableEnemyWithRarity> DaytimeEnemies { get; }
    }

    private sealed class RouteNodeState
    {
        public RouteNodeState(int itemCost)
        {
            ItemCost = itemCost;
        }

        public int ItemCost { get; }
    }
}
