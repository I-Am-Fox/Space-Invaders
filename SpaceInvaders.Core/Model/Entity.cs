using CoreMath = SpaceInvaders.Core.Primitives;

namespace SpaceInvaders.Core.Model;

public sealed class Entity
{
    public Entity(int id, EntityKind kind, CoreMath.GridPoint pos)
    {
        Id = id;
        Kind = kind;
        Pos = pos;
    }

    public int Id { get; }
    public EntityKind Kind { get; }

    public CoreMath.GridPoint Pos { get; set; }

    public int Hp { get; set; } = 1;
    public int Damage { get; set; } = 1;
}
