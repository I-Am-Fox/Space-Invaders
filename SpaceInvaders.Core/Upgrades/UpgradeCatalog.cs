namespace SpaceInvaders.Core.Upgrades;

public static class UpgradeCatalog
{
    public static readonly IReadOnlyList<Upgrade> All = new List<Upgrade>
    {
        // Common (grey): no debuffs.
        Validate(new Upgrade(
            UpgradeId.MoreHp,
            UpgradeRarity.Common,
            "Reinforced Hull",
            "+1 max HP",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.MaxHp, +1) },
            Debuffs: Array.Empty<UpgradeEffect>()
        )),

        Validate(new Upgrade(
            UpgradeId.MoreDamage,
            UpgradeRarity.Common,
            "High-Energy Rounds",
            "+1 bullet damage",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.BulletDamageBonus, +1) },
            Debuffs: Array.Empty<UpgradeEffect>()
        )),

        // Rare (blue): can have debuffs.
        Validate(new Upgrade(
            UpgradeId.FasterFire,
            UpgradeRarity.Rare,
            "Overclocked Cannon",
            "-1 cooldown / -1 max HP",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.FireCooldownTicks, -1) },
            Debuffs: new[] { new UpgradeEffect(UpgradeStat.MaxHp, -1) }
        )),

        Validate(new Upgrade(
            UpgradeId.FasterMove,
            UpgradeRarity.Rare,
            "Thruster Boost",
            "+1 move speed / +1 cooldown",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.MoveSpeed, +1) },
            Debuffs: new[] { new UpgradeEffect(UpgradeStat.FireCooldownTicks, +1) }
        )),

        // Very rare (purple): stronger, can have debuffs.
        Validate(new Upgrade(
            UpgradeId.DoubleShot,
            UpgradeRarity.VeryRare,
            "Split Barrel",
            "+1 projectile / +1 cooldown",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.ShotsPerPress, +1) },
            Debuffs: new[] { new UpgradeEffect(UpgradeStat.FireCooldownTicks, +1) }
        )),

        // Legendary (gold): powerful, can have debuffs.
        Validate(new Upgrade(
            UpgradeId.GoldenContract,
            UpgradeRarity.Legendary,
            "Golden Contract",
            "+50% coins per wave / +1 cooldown",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.CoinsPerWaveMultiplierPct, +50) },
            Debuffs: new[] { new UpgradeEffect(UpgradeStat.FireCooldownTicks, +1) }
        )),

        Validate(new Upgrade(
            UpgradeId.TemporalDrag,
            UpgradeRarity.Legendary,
            "Temporal Drag",
            "Aliens move 30% slower / -1 max HP",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.AlienMoveSpeedPct, -30) },
            Debuffs: new[] { new UpgradeEffect(UpgradeStat.MaxHp, -1) }
        )),

        Validate(new Upgrade(
            UpgradeId.ArmorPlating,
            UpgradeRarity.Legendary,
            "Armor Plating",
            "+2 max HP / -1 move speed",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.MaxHp, +2) },
            Debuffs: new[] { new UpgradeEffect(UpgradeStat.MoveSpeed, -1) }
        )),

        // Mythic (red): extremely strong, NO debuffs.
        Validate(new Upgrade(
            UpgradeId.CoinMagnet,
            UpgradeRarity.Mythic,
            "Coin Magnet",
            "+3 coins per wave +100% coins per wave",
            Buffs: new[]
            {
                new UpgradeEffect(UpgradeStat.CoinsPerWaveBonus, +3),
                new UpgradeEffect(UpgradeStat.CoinsPerWaveMultiplierPct, +100)
            },
            Debuffs: Array.Empty<UpgradeEffect>()
        )),

        Validate(new Upgrade(
            UpgradeId.VoidRounds,
            UpgradeRarity.Mythic,
            "Void Rounds",
            "+3 bullet damage",
            Buffs: new[] { new UpgradeEffect(UpgradeStat.BulletDamageBonus, +3) },
            Debuffs: Array.Empty<UpgradeEffect>()
        )),
    };

    private static Upgrade Validate(Upgrade u)
    {
        // Rule: only blue/purple/gold can come with debuffs.
        if (!u.IsDebuffAllowedByRarity && u.Debuffs.Count > 0)
            throw new InvalidOperationException($"Upgrade '{u.Name}' has debuffs but rarity is {u.Rarity}.");

        return u;
    }
}
