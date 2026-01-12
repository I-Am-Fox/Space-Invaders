namespace SpaceInvaders.Core.Upgrades;

/// <summary>
/// A single stat change applied by an upgrade (positive for buffs, negative for debuffs).
/// Keep it simple and deterministic.
/// </summary>
public sealed record UpgradeEffect(UpgradeStat Stat, int Delta);

public enum UpgradeStat
{
    MaxHp,
    FireCooldownTicks,
    ShotsPerPress,
    BulletDamageBonus,
    MoveSpeed,

    // Economy and difficulty modifiers
    CoinsPerWaveBonus,
    CoinsPerWaveMultiplierPct,
    AlienMoveSpeedPct
}
