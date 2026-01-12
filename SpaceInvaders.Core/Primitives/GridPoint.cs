namespace SpaceInvaders.Core.Primitives;

public readonly record struct GridPoint(int X, int Y)
{
    public static GridPoint operator +(GridPoint a, GridPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static GridPoint operator -(GridPoint a, GridPoint b) => new(a.X - b.X, a.Y - b.Y);
}
