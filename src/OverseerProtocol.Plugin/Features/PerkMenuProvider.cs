using System.Linq;
using System.Text;

namespace OverseerProtocol.Features;

internal sealed class PerkMenuProvider
{
    public string BuildReadOnlySummary()
    {
        var catalog = new PerkCatalogFeature().LoadOrCreate();
        var progression = new ProgressionStore().LoadOrCreate();
        var text = new StringBuilder();

        text.AppendLine("Ship progression");
        text.AppendLine("Level: " + progression.Ship.Level + "  XP: " + progression.Ship.Experience + "  Points: " + progression.Ship.UnspentPoints);
        text.AppendLine("Unlocked ship ranks: " + progression.Ship.PerkRanks.Count);
        text.AppendLine();
        text.AppendLine("Ship perks");
        foreach (var perk in catalog.ShipPerks.OrderBy(perk => perk.DisplayName))
        {
            progression.Ship.PerkRanks.TryGetValue(perk.Id, out var rank);
            text.AppendLine("- " + perk.DisplayName + " [" + rank + "/" + perk.MaxRank + "]");
            text.AppendLine("  " + perk.Description);
            text.AppendLine("  Costs: " + string.Join(", ", perk.RankCosts));
            text.AppendLine("  Effects: " + FormatEffects(perk.Effects));
        }

        text.AppendLine();
        text.AppendLine("Players");
        if (progression.Players.Count == 0)
        {
            text.AppendLine("- No player progression saved yet.");
        }
        else
        {
            foreach (var player in progression.Players.OrderBy(player => player.DisplayName))
                text.AppendLine("- " + player.DisplayName + " level " + player.Level + " XP " + player.Experience + " points " + player.UnspentPoints + " ranks " + player.PerkRanks.Count);
        }

        text.AppendLine();
        text.AppendLine("Player perks");
        foreach (var perk in catalog.PlayerPerks.OrderBy(perk => perk.DisplayName))
        {
            text.AppendLine("- " + perk.DisplayName + " [max " + perk.MaxRank + "]");
            text.AppendLine("  " + perk.Description);
            text.AppendLine("  Costs: " + string.Join(", ", perk.RankCosts));
            text.AppendLine("  Effects: " + FormatEffects(perk.Effects));
        }

        return text.ToString();
    }

    private static string FormatEffects(System.Collections.Generic.List<OverseerProtocol.Data.Models.Perks.PerkRankEffect> effects)
    {
        if (effects.Count == 0)
            return "none";

        return string.Join(
            ", ",
            effects
                .OrderBy(effect => effect.Rank)
                .Select(effect => "r" + effect.Rank + " " + effect.Stat + " x" + effect.Multiplier.ToString("0.###")));
    }
}
