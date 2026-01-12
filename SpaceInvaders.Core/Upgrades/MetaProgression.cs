namespace SpaceInvaders.Core.Upgrades;

/// <summary>
/// Persistent progression outside a single run ("meta" upgrades).
/// </summary>
public sealed class MetaProgression
{
    public int Coins { get; set; }

    // Permanent upgrades
    public int MaxHpLevel { get; set; }
    public int DamageLevel { get; set; }
    public int FireRateLevel { get; set; }
    public int MoveSpeedLevel { get; set; }

    // Audio settings
    public float MusicVolume { get; set; } = 0.35f;
    public float SfxVolume { get; set; } = 0.85f;
    public bool IsMuted { get; set; }

    // UI scaling
    public bool UiScaleAuto { get; set; } = true;
    public float UiScale { get; set; } = 1.0f; // used when UiScaleAuto == false

    public static MetaProgression Default { get; } = new();
}
