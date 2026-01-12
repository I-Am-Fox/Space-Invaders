namespace SpaceInvaders.Core.Upgrades;

public sealed record MetaUpgrade(
    MetaUpgradeId Id,
    string Name,
    string Description,
    int MaxLevel,
    Func<MetaProgression, int> GetLevel,
    Action<MetaProgression> IncreaseLevel,
    Func<int, int> CostForNextLevel)
{
    public bool CanPurchase(MetaProgression meta)
    {
        var lvl = GetLevel(meta);
        return lvl < MaxLevel && meta.Coins >= CostForNextLevel(lvl + 1);
    }

    public bool TryPurchase(MetaProgression meta)
    {
        var lvl = GetLevel(meta);
        if (lvl >= MaxLevel) return false;

        var cost = CostForNextLevel(lvl + 1);
        if (meta.Coins < cost) return false;

        meta.Coins -= cost;
        IncreaseLevel(meta);
        return true;
    }
}
