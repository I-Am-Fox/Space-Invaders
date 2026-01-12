using System.Collections.Generic;
using System.Linq;
using CoreMath = SpaceInvaders.Core.Primitives;

namespace SpaceInvaders.Core.Model;

public sealed class GameState
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    public required RunState Run { get; init; }

    public bool IsGameOver { get; set; }
    public bool IsPaused { get; set; }
    public string? StatusLine { get; set; }

    public required Entity Player { get; init; }

    public List<Entity> Entities { get; } = new();

    public IEnumerable<Entity> Aliens => Entities.Where(e => e.Kind == EntityKind.Alien);
    public IEnumerable<Entity> Bullets => Entities.Where(e => e.Kind is EntityKind.PlayerBullet or EntityKind.AlienBullet);

    public int ClampX(int x) => System.Math.Clamp(x, 0, Width - 1);

    public bool InBounds(CoreMath.GridPoint p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
}
