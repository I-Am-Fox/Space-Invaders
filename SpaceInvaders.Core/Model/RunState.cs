using SpaceInvaders.Core.Upgrades;

namespace SpaceInvaders.Core.Model;

public sealed class RunState
{
    public int Wave { get; set; } = 1;
    public int Score { get; set; }

    // Roguelike progression
    public int Credits { get; set; }

    // Stats modified by upgrades
    public int ShotsPerPress { get; set; } = 1;
    public int BulletDamageBonus { get; set; }
    public int FireCooldownTicks { get; set; } = 6;
    public int MoveSpeed { get; set; } = 1;
    public int PlayerMaxHp { get; set; } = 3;

    // Economy scaling
    public int CoinsPerWaveBonus { get; set; }
    public int CoinsPerWaveMultiplierPct { get; set; } = 100;

    // Enemy speed scaling
    public int AlienMoveSpeedPct { get; set; } = 100;

    public HashSet<UpgradeId> PickedUpgrades { get; } = new();
}
