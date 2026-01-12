namespace SpaceInvaders.Core.Upgrades;

public static class MetaUpgradeCatalog
{
    public static readonly IReadOnlyList<MetaUpgrade> All = new List<MetaUpgrade>
    {
        new(
            MetaUpgradeId.MaxHp,
            "Reinforced Hull",
            "+1 starting max HP per level",
            MaxLevel: 5,
            GetLevel: m => m.MaxHpLevel,
            IncreaseLevel: m => m.MaxHpLevel++,
            CostForNextLevel: lvl => 10 * lvl),

        new(
            MetaUpgradeId.Damage,
            "High Energy Rounds",
            "+1 starting bullet damage bonus per level",
            MaxLevel: 5,
            GetLevel: m => m.DamageLevel,
            IncreaseLevel: m => m.DamageLevel++,
            CostForNextLevel: lvl => 12 * lvl),

        new(
            MetaUpgradeId.FireRate,
            "Overclocked Cannon",
            "-1 starting fire cooldown ticks per level (min capped in-game)",
            MaxLevel: 5,
            GetLevel: m => m.FireRateLevel,
            IncreaseLevel: m => m.FireRateLevel++,
            CostForNextLevel: lvl => 14 * lvl),

        new(
            MetaUpgradeId.MoveSpeed,
            "Thruster Boost",
            "+1 starting move speed (every 2 levels)",
            MaxLevel: 6,
            GetLevel: m => m.MoveSpeedLevel,
            IncreaseLevel: m => m.MoveSpeedLevel++,
            CostForNextLevel: lvl => 8 * lvl)
    };
}
