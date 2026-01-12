namespace SpaceInvaders.Core.Engine;

public sealed record GameConfig(
    int Width,
    int Height,
    int MaxPlayerHp,
    int StartingWave,
    int Seed,
    double TickSeconds)
{
    public static GameConfig Default { get; } = new(
        Width: 40,
        Height: 24,
        MaxPlayerHp: 3,
        StartingWave: 1,
        Seed: System.Environment.TickCount,
        TickSeconds: 0.05);
}
