namespace SpaceInvaders.Core.Upgrades;

public static class MetaApplication
{
    public static void ApplyToRun(MetaProgression meta, global::SpaceInvaders.Core.Model.RunState run)
    {
        // Starting boosts
        run.PlayerMaxHp += meta.MaxHpLevel;
        run.BulletDamageBonus += meta.DamageLevel;

        // Fire cooldown ticks: lower is faster
        run.FireCooldownTicks = global::System.Math.Max(1, run.FireCooldownTicks - meta.FireRateLevel);

        // Every 2 levels add +1 move speed
        run.MoveSpeed += meta.MoveSpeedLevel / 2;
    }

    public static int CoinsEarnedForWaveClear(int wave) => 3 + (wave / 3);
}
