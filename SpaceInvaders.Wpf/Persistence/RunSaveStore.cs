using System;
using System.IO;
using System.Text.Json;
using SpaceInvaders.Core.Engine;
using SpaceInvaders.Core.Model;
using SpaceInvaders.Core.Upgrades;

namespace SpaceInvaders.Wpf.Persistence;

/// <summary>
/// Persists an in-progress run so it can be resumed after exiting the app.
/// Stored separately from the meta profile.
/// </summary>
public sealed class RunSaveStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SpaceInvaders",
        "run-save.json");

    public bool HasSave() => File.Exists(SavePath);

    public void Clear()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch
        {
            // ignore
        }
    }

    public void Save(GameConfig config, GameState state, IReadOnlyList<Upgrade>? pendingUpgrades)
    {
        try
        {
            var dir = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var dto = RunSaveDto.From(config, state, pendingUpgrades);
            var json = JsonSerializer.Serialize(dto, Options);
            File.WriteAllText(SavePath, json);
        }
        catch
        {
            // ignore
        }
    }

    public RunSaveDto? Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            var json = File.ReadAllText(SavePath);
            return JsonSerializer.Deserialize<RunSaveDto>(json, Options);
        }
        catch
        {
            return null;
        }
    }

    public sealed class RunSaveDto
    {
        public required GameConfig Config { get; init; }
        public required GameStateDto State { get; init; }
        public List<UpgradeDto>? PendingUpgrades { get; init; }

        public static RunSaveDto From(GameConfig config, GameState state, IReadOnlyList<Upgrade>? pendingUpgrades)
        {
            return new RunSaveDto
            {
                Config = config,
                State = GameStateDto.From(state),
                PendingUpgrades = pendingUpgrades?.Select(UpgradeDto.From).ToList()
            };
        }
    }

    public sealed class GameStateDto
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required RunStateDto Run { get; init; }
        public bool IsGameOver { get; init; }
        public bool IsPaused { get; init; }
        public string? StatusLine { get; init; }
        public required EntityDto Player { get; init; }
        public required List<EntityDto> Entities { get; init; }

        public static GameStateDto From(GameState state) => new()
        {
            Width = state.Width,
            Height = state.Height,
            Run = RunStateDto.From(state.Run),
            IsGameOver = state.IsGameOver,
            IsPaused = state.IsPaused,
            StatusLine = state.StatusLine,
            Player = EntityDto.From(state.Player),
            Entities = state.Entities.Select(EntityDto.From).ToList()
        };

        public GameState ToModel()
        {
            var player = Player.ToModel();
            var state = new GameState
            {
                Width = Width,
                Height = Height,
                Run = Run.ToModel(),
                IsGameOver = IsGameOver,
                IsPaused = IsPaused,
                StatusLine = StatusLine,
                Player = player,
            };

            // Entities list includes player in this game; keep behavior consistent.
            state.Entities.AddRange(Entities.Select(e => e.ToModel()));
            if (!state.Entities.Any(e => e.Id == player.Id))
                state.Entities.Add(player);

            return state;
        }
    }

    public sealed class RunStateDto
    {
        public int Wave { get; init; }
        public int Score { get; init; }
        public int Credits { get; init; }

        public int ShotsPerPress { get; init; }
        public int BulletDamageBonus { get; init; }
        public int FireCooldownTicks { get; init; }
        public int MoveSpeed { get; init; }
        public int PlayerMaxHp { get; init; }

        public int CoinsPerWaveBonus { get; init; }
        public int CoinsPerWaveMultiplierPct { get; init; }
        public int AlienMoveSpeedPct { get; init; }

        public List<UpgradeId> PickedUpgrades { get; init; } = new();

        public static RunStateDto From(RunState run) => new()
        {
            Wave = run.Wave,
            Score = run.Score,
            Credits = run.Credits,
            ShotsPerPress = run.ShotsPerPress,
            BulletDamageBonus = run.BulletDamageBonus,
            FireCooldownTicks = run.FireCooldownTicks,
            MoveSpeed = run.MoveSpeed,
            PlayerMaxHp = run.PlayerMaxHp,
            CoinsPerWaveBonus = run.CoinsPerWaveBonus,
            CoinsPerWaveMultiplierPct = run.CoinsPerWaveMultiplierPct,
            AlienMoveSpeedPct = run.AlienMoveSpeedPct,
            PickedUpgrades = run.PickedUpgrades.ToList()
        };

        public RunState ToModel()
        {
            var run = new RunState
            {
                Wave = Wave,
                Score = Score,
                Credits = Credits,
                ShotsPerPress = ShotsPerPress,
                BulletDamageBonus = BulletDamageBonus,
                FireCooldownTicks = FireCooldownTicks,
                MoveSpeed = MoveSpeed,
                PlayerMaxHp = PlayerMaxHp,
                CoinsPerWaveBonus = CoinsPerWaveBonus,
                CoinsPerWaveMultiplierPct = CoinsPerWaveMultiplierPct,
                AlienMoveSpeedPct = AlienMoveSpeedPct,
            };

            foreach (var id in PickedUpgrades)
                run.PickedUpgrades.Add(id);

            return run;
        }
    }

    public sealed class EntityDto
    {
        public int Id { get; init; }
        public EntityKind Kind { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Hp { get; init; }
        public int Damage { get; init; }

        public static EntityDto From(Entity e) => new()
        {
            Id = e.Id,
            Kind = e.Kind,
            X = e.Pos.X,
            Y = e.Pos.Y,
            Hp = e.Hp,
            Damage = e.Damage
        };

        public Entity ToModel()
        {
            return new Entity(Id, Kind, new SpaceInvaders.Core.Primitives.GridPoint(X, Y))
            {
                Hp = Hp,
                Damage = Damage
            };
        }
    }

    public sealed class UpgradeDto
    {
        public required UpgradeId Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required UpgradeRarity Rarity { get; init; }

        public static UpgradeDto From(Upgrade u) => new()
        {
            Id = u.Id,
            Name = u.Name,
            Description = u.Description,
            Rarity = u.Rarity
        };
    }
}

