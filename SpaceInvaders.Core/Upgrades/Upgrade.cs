using SpaceInvaders.Core.Model;

namespace SpaceInvaders.Core.Upgrades;

public sealed record Upgrade(
    UpgradeId Id,
    UpgradeRarity Rarity,
    string Name,
    string Description,
    IReadOnlyList<UpgradeEffect> Buffs,
    IReadOnlyList<UpgradeEffect> Debuffs)
{
    public void Apply(RunState run)
    {
        foreach (var e in Buffs)
            ApplyEffect(run, e);

        foreach (var e in Debuffs)
            ApplyEffect(run, e);

        // Global clamps / sanity
        run.FireCooldownTicks = System.Math.Max(1, run.FireCooldownTicks);
        run.ShotsPerPress = System.Math.Clamp(run.ShotsPerPress, 1, 3);
        run.MoveSpeed = System.Math.Clamp(run.MoveSpeed, 1, 3);
        run.PlayerMaxHp = System.Math.Max(1, run.PlayerMaxHp);
    }

    public bool IsDebuffAllowedByRarity => Rarity is UpgradeRarity.Rare or UpgradeRarity.VeryRare or UpgradeRarity.Legendary;

    private static void ApplyEffect(RunState run, UpgradeEffect e)
    {
        switch (e.Stat)
        {
            case UpgradeStat.MaxHp:
                run.PlayerMaxHp += e.Delta;
                break;
            case UpgradeStat.FireCooldownTicks:
                run.FireCooldownTicks += e.Delta;
                break;
            case UpgradeStat.ShotsPerPress:
                run.ShotsPerPress += e.Delta;
                break;
            case UpgradeStat.BulletDamageBonus:
                run.BulletDamageBonus += e.Delta;
                break;
            case UpgradeStat.MoveSpeed:
                run.MoveSpeed += e.Delta;
                break;

            case UpgradeStat.CoinsPerWaveBonus:
                run.CoinsPerWaveBonus += e.Delta;
                break;
            case UpgradeStat.CoinsPerWaveMultiplierPct:
                run.CoinsPerWaveMultiplierPct += e.Delta;
                break;
            case UpgradeStat.AlienMoveSpeedPct:
                run.AlienMoveSpeedPct += e.Delta;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(e.Stat), e.Stat, null);
        }
    }
}
