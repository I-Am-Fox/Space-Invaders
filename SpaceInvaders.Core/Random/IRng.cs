namespace SpaceInvaders.Core.Random;

public interface IRng
{
    int NextInt(int min, int max);
    double NextDouble();
}
