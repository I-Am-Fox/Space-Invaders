using System.Collections.Generic;
using System.Linq;
using SpaceInvaders.Core.Model;
using SpaceInvaders.Core.Random;

namespace SpaceInvaders.Core.Upgrades;

public static class UpgradeDraft
{
    public static IReadOnlyList<Upgrade> Draft(IRng rng, RunState run, int count)
    {
        var pool = UpgradeCatalog.All
            .Where(u => !run.PickedUpgrades.Contains(u.Id))
            .ToList();

        // Lock shots-per-press to 2 max, and once reached,
        // the DoubleShot upgrade should never appear.
        if (run.ShotsPerPress >= 2)
            pool.RemoveAll(u => u.Id == UpgradeId.DoubleShot);

        var chosen = new List<Upgrade>(count);

        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var pick = PickWeightedByRarity(rng, pool);
            chosen.Add(pick);
            pool.Remove(pick);
        }

        return chosen;
    }

    private static Upgrade PickWeightedByRarity(IRng rng, List<Upgrade> pool)
    {
        // Higher weight => more common.
        // Tune later; these are reasonable defaults.
        static int Weight(UpgradeRarity r) => r switch
        {
            UpgradeRarity.Common => 70,
            UpgradeRarity.Rare => 22,
            UpgradeRarity.VeryRare => 7,
            UpgradeRarity.Legendary => 1,
            UpgradeRarity.Mythic => 1, // extremely rare but possible
            _ => 1
        };

        if (pool.Count == 1) return pool[0];

        var weights = new int[pool.Count];
        var total = 0;

        for (var i = 0; i < pool.Count; i++)
        {
            var w = System.Math.Max(0, Weight(pool[i].Rarity));
            weights[i] = w;
            total += w;
        }

        // If all weights are 0, just pick uniformly.
        if (total <= 0)
            return pool[rng.NextInt(0, pool.Count)];

        var roll = rng.NextInt(0, total);
        for (var i = 0; i < pool.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0) return pool[i];
        }

        return pool[^1];
    }
}
