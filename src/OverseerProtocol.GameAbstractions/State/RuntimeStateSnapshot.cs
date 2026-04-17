using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.State;

public sealed class RuntimeStateSnapshot
{
    private readonly Dictionary<string, ItemState> _items = new();
    private readonly Dictionary<string, LevelSpawnState> _levels = new();
    private readonly Dictionary<string, RouteNodeState> _routeNodes = new();
    private Item[]? _terminalBuyableItems;
    private int[]? _terminalItemSalesPercentages;

    public bool IsCaptured { get; private set; }

    public void Capture()
    {
        _items.Clear();
        _levels.Clear();
        _routeNodes.Clear();

        var itemCount = CaptureItems();
        var levelCount = CaptureLevels();
        var routeNodeCount = CaptureRouteNodes();
        CaptureTerminalStore();

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
        RestoreTerminalStore();

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

            _items[item.name] = new ItemState(item.weight, item.creditsWorth, item.minValue, item.maxValue, item.isScrap, item.requiresBattery);
            OPLog.Debug("Snapshot", $"Captured item state: item={item.name}, weight={item.weight:0.###}, creditsWorth={item.creditsWorth}, minValue={item.minValue}, maxValue={item.maxValue}, isScrap={item.isScrap}, requiresBattery={item.requiresBattery}");
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
                level.LevelDescription,
                level.minScrap,
                level.maxScrap,
                ClonePool(level.Enemies),
                ClonePool(level.OutsideEnemies),
                ClonePool(level.DaytimeEnemies));
            OPLog.Debug(
                "Snapshot",
                $"Captured moon state: moon={level.name}, riskLevel={level.riskLevel}, minScrap={level.minScrap}, maxScrap={level.maxScrap}, inside={level.Enemies?.Count ?? 0}, outside={level.OutsideEnemies?.Count ?? 0}, daytime={level.DaytimeEnemies?.Count ?? 0}");
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
            OPLog.Debug("Snapshot", $"Captured route node state: node={node.name}, moonIndex={node.buyRerouteToMoon}, itemCost={node.itemCost}");
        }

        return _routeNodes.Count;
    }

    private void CaptureTerminalStore()
    {
        var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
        if (terminal == null)
        {
            OPLog.Warning("Snapshot", "Terminal not available. Store snapshot skipped.");
            return;
        }

        _terminalBuyableItems = terminal.buyableItemsList == null
            ? null
            : (Item[])terminal.buyableItemsList.Clone();
        _terminalItemSalesPercentages = terminal.itemSalesPercentages == null
            ? null
            : (int[])terminal.itemSalesPercentages.Clone();
        OPLog.Info("Snapshot", $"Captured terminal store state: buyableItems={_terminalBuyableItems?.Length ?? 0}, sales={_terminalItemSalesPercentages?.Length ?? 0}");
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
            var beforeMin = item.minValue;
            var beforeMax = item.maxValue;
            var beforeScrap = item.isScrap;
            var beforeBattery = item.requiresBattery;
            item.weight = state.Weight;
            item.creditsWorth = state.CreditsWorth;
            item.minValue = state.MinValue;
            item.maxValue = state.MaxValue;
            item.isScrap = state.IsScrap;
            item.requiresBattery = state.RequiresBattery;
            OPLog.Debug(
                "Snapshot",
                $"Restored item state: item={item.name}, weight {beforeWeight:0.###} -> {item.weight:0.###}, creditsWorth {beforeCredits} -> {item.creditsWorth}, minValue {beforeMin} -> {item.minValue}, maxValue {beforeMax} -> {item.maxValue}, isScrap {beforeScrap} -> {item.isScrap}, requiresBattery {beforeBattery} -> {item.requiresBattery}");
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
            var beforeMin = level.minScrap;
            var beforeMax = level.maxScrap;
            var beforeInside = level.Enemies?.Count ?? 0;
            var beforeOutside = level.OutsideEnemies?.Count ?? 0;
            var beforeDaytime = level.DaytimeEnemies?.Count ?? 0;
            level.riskLevel = state.RiskLevel;
            level.LevelDescription = state.Description;
            level.minScrap = state.MinScrap;
            level.maxScrap = state.MaxScrap;
            level.Enemies = ClonePool(state.InsideEnemies);
            level.OutsideEnemies = ClonePool(state.OutsideEnemies);
            level.DaytimeEnemies = ClonePool(state.DaytimeEnemies);
            OPLog.Debug(
                "Snapshot",
                $"Restored moon state: moon={level.name}, riskLevel {beforeRisk} -> {level.riskLevel}, minScrap {beforeMin} -> {level.minScrap}, maxScrap {beforeMax} -> {level.maxScrap}, inside {beforeInside} -> {level.Enemies.Count}, outside {beforeOutside} -> {level.OutsideEnemies.Count}, daytime {beforeDaytime} -> {level.DaytimeEnemies.Count}");
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
            OPLog.Debug("Snapshot", $"Restored route node state: node={node.name}, itemCost {before} -> {node.itemCost}");
            restored++;
        }

        return restored;
    }

    private void RestoreTerminalStore()
    {
        var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
        if (terminal == null)
        {
            OPLog.Warning("Snapshot", "Terminal not available. Store restore skipped.");
            return;
        }

        if (_terminalBuyableItems != null)
            terminal.buyableItemsList = (Item[])_terminalBuyableItems.Clone();

        if (_terminalItemSalesPercentages != null)
            terminal.itemSalesPercentages = (int[])_terminalItemSalesPercentages.Clone();

        OPLog.Info("Snapshot", $"Restored terminal store state: buyableItems={terminal.buyableItemsList?.Length ?? 0}, sales={terminal.itemSalesPercentages?.Length ?? 0}");
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
        public ItemState(float weight, int creditsWorth, int minValue, int maxValue, bool isScrap, bool requiresBattery)
        {
            Weight = weight;
            CreditsWorth = creditsWorth;
            MinValue = minValue;
            MaxValue = maxValue;
            IsScrap = isScrap;
            RequiresBattery = requiresBattery;
        }

        public float Weight { get; }
        public int CreditsWorth { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public bool IsScrap { get; }
        public bool RequiresBattery { get; }
    }

    private sealed class LevelSpawnState
    {
        public LevelSpawnState(
            string riskLevel,
            string description,
            int minScrap,
            int maxScrap,
            List<SpawnableEnemyWithRarity> insideEnemies,
            List<SpawnableEnemyWithRarity> outsideEnemies,
            List<SpawnableEnemyWithRarity> daytimeEnemies)
        {
            RiskLevel = riskLevel;
            Description = description;
            MinScrap = minScrap;
            MaxScrap = maxScrap;
            InsideEnemies = insideEnemies;
            OutsideEnemies = outsideEnemies;
            DaytimeEnemies = daytimeEnemies;
        }

        public string RiskLevel { get; }
        public string Description { get; }
        public int MinScrap { get; }
        public int MaxScrap { get; }
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
