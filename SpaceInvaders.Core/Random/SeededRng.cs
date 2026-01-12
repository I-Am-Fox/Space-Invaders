using System;

namespace SpaceInvaders.Core.Random;

/// <summary>
/// Deterministic RNG (wraps System.Random with an explicit seed).
/// </summary>
public sealed class SeededRng : IRng
{
    private readonly System.Random _random;

    public SeededRng(int seed) => _random = new System.Random(seed);

    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
