using System.Linq;
using SpaceInvaders.Core.Model;
using SpaceInvaders.Core.Primitives;
using SpaceInvaders.Core.Random;
using SpaceInvaders.Core.Upgrades;

namespace SpaceInvaders.Core.Engine;

/// <summary>
/// Deterministic, step-based simulation. UI should feed commands and render GameState.
/// </summary>
public sealed class Game
{
    private readonly IRng _rng;

    private int _nextEntityId = 1;

    private int _fireCooldownRemaining;
    private int _alienMoveDir = 1; // 1 right, -1 left
    private int _alienMoveTick;

    public GameState State { get; }

    public IReadOnlyList<Upgrade>? PendingUpgrades { get; private set; }

    public Game(GameConfig config)
    {
        _rng = new SeededRng(config.Seed);

        var run = new RunState
        {
            Wave = config.StartingWave,
            PlayerMaxHp = config.MaxPlayerHp,
        };

        var player = new Entity(AllocateId(), EntityKind.Player,
            new GridPoint(config.Width / 2, config.Height - 2))
        {
            Hp = run.PlayerMaxHp,
        };

        State = new GameState
        {
            Width = config.Width,
            Height = config.Height,
            Run = run,
            Player = player,
            StatusLine = "Arrows/A-D move, Space shoot, 1/2/3 pick upgrade, Q quit",
        };

        SpawnWave();
    }

    public void Step(GameCommand command, UpgradeId? pickedUpgrade = null)
    {
        if (State.IsGameOver) return;

        if (PendingUpgrades is not null)
        {
            if (pickedUpgrade is null)
            {
                // no-op until user picks
                return;
            }

            var up = PendingUpgrades.FirstOrDefault(u => u.Id == pickedUpgrade);
            if (up is null) return;

            up.Apply(State.Run);

            // Enforce design constraint: shots-per-press is capped at 2 for this game mode.
            if (State.Run.ShotsPerPress > 2)
                State.Run.ShotsPerPress = 2;

            State.Run.PickedUpgrades.Add(up.Id);

            State.Player.Hp = Math.Min(State.Player.Hp + 1, State.Run.PlayerMaxHp);
            PendingUpgrades = null;
            State.StatusLine = $"Applied: {up.Name}";
            return;
        }

        if (command == GameCommand.Quit)
        {
            State.IsGameOver = true;
            State.StatusLine = "Quit.";
            return;
        }

        if (command == GameCommand.Pause)
        {
            State.IsPaused = !State.IsPaused;
            return;
        }

        if (State.IsPaused) return;

        HandlePlayer(command);
        HandleAliens();
        HandleBullets();
        ResolveCollisionsAndCleanup();

        if (!State.Aliens.Any())
        {
            State.Run.Wave++;

            var baseCoins = MetaApplication.CoinsEarnedForWaveClear(State.Run.Wave);
            var scaledCoins = (baseCoins * Math.Max(0, State.Run.CoinsPerWaveMultiplierPct)) / 100;
            scaledCoins += State.Run.CoinsPerWaveBonus;
            if (scaledCoins < 0) scaledCoins = 0;

            State.Run.Credits += scaledCoins;

            // Upgrades only after boss waves.
            const int bossEvery = 5;
            if (State.Run.Wave % bossEvery == 0)
            {
                PendingUpgrades = UpgradeDraft.Draft(_rng, State.Run, 5);
                State.StatusLine = $"Boss wave cleared! Pick an upgrade: {string.Join(" / ", PendingUpgrades.Select((u, i) => $"{i + 1}:{u.Name}"))}";
            }
            else
            {
                PendingUpgrades = null;
                State.StatusLine = $"Wave cleared!";
            }

            SpawnWave();
        }

        if (State.Player.Hp <= 0)
        {
            State.IsGameOver = true;
            State.StatusLine = "Game Over.";
        }
    }

    private int AllocateId() => _nextEntityId++;

    private void SpawnWave()
    {
        // Clear non-player entities
        State.Entities.RemoveAll(e => e.Kind != EntityKind.Player);

        // Basic invader block; difficulty scales with wave.
        // Keep early waves manageable; ramp up later.
        var cols = Math.Min(10, 5 + (State.Run.Wave / 2));
        var rows = Math.Min(5, 2 + (State.Run.Wave - 1) / 3);

        var startX = Math.Max(0, (State.Width - cols * 3) / 2);
        var startY = 2;

        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
        {
            var pos = new GridPoint(startX + c * 3, startY + r * 2);
            var alien = new Entity(AllocateId(), EntityKind.Alien, pos)
            {
                // HP increases every 6 waves (so waves 1-6 are 1 HP), then ramps.
                Hp = 1 + (State.Run.Wave - 1) / 6,
                Damage = 1
            };
            State.Entities.Add(alien);
        }

        // Player setup each wave
        State.Player.Pos = new GridPoint(State.Width / 2, State.Height - 2);
        State.Player.Hp = Math.Min(State.Player.Hp, State.Run.PlayerMaxHp);
        if (!State.Entities.Contains(State.Player))
            State.Entities.Add(State.Player);

        _alienMoveDir = 1;
        _alienMoveTick = 0;
        _fireCooldownRemaining = 0;
    }

    private void HandlePlayer(GameCommand command)
    {
        var move = 0;
        if (command == GameCommand.MoveLeft) move = -State.Run.MoveSpeed;
        if (command == GameCommand.MoveRight) move = State.Run.MoveSpeed;

        if (move != 0)
        {
            State.Player.Pos = new GridPoint(State.ClampX(State.Player.Pos.X + move), State.Player.Pos.Y);
        }

        if (_fireCooldownRemaining > 0) _fireCooldownRemaining--;

        if (command == GameCommand.Shoot && _fireCooldownRemaining == 0)
        {
            _fireCooldownRemaining = State.Run.FireCooldownTicks;

            var shots = State.Run.ShotsPerPress;
            var spread = shots == 1 ? new[] { 0 } : shots == 2 ? new[] { -1, 1 } : new[] { -1, 0, 1 };

            foreach (var dx in spread)
            {
                var bpos = new GridPoint(State.ClampX(State.Player.Pos.X + dx), State.Player.Pos.Y - 1);
                if (!State.InBounds(bpos)) continue;

                var bullet = new Entity(AllocateId(), EntityKind.PlayerBullet, bpos)
                {
                    Damage = 1 + State.Run.BulletDamageBonus,
                    Hp = 1
                };
                State.Entities.Add(bullet);
            }
        }
    }

    private void HandleAliens()
    {
        _alienMoveTick++;

        // Base speed up with wave; then scale by run modifier.
        // Slower early ramp: 12 -> 6 by wave 6, then continues down to a minimum.
        var baseMoveEvery = Math.Max(2, 12 - (State.Run.Wave * 1));
        var moveEvery = (baseMoveEvery * 100) / Math.Max(10, State.Run.AlienMoveSpeedPct);
        moveEvery = Math.Max(1, moveEvery);

        if (_alienMoveTick < moveEvery) return;
        _alienMoveTick = 0;

        var aliens = State.Aliens.ToList();
        if (aliens.Count == 0) return;

        var minX = aliens.Min(a => a.Pos.X);
        var maxX = aliens.Max(a => a.Pos.X);

        var wouldHitEdge = _alienMoveDir == 1 ? maxX + 1 >= State.Width - 1 : minX - 1 <= 0;

        if (wouldHitEdge)
        {
            _alienMoveDir *= -1;
            foreach (var a in aliens)
                a.Pos = new GridPoint(a.Pos.X, a.Pos.Y + 1);
        }
        else
        {
            foreach (var a in aliens)
                a.Pos = new GridPoint(a.Pos.X + _alienMoveDir, a.Pos.Y);
        }

        // Random shooting from bottom-most aliens in each column
        // Much gentler early ramp.
        var shootChance = Math.Min(0.25, 0.03 + State.Run.Wave * 0.008);
        if (_rng.NextDouble() > shootChance) return;

        var groupedByX = aliens
            .GroupBy(a => a.Pos.X)
            .Select(g => g.OrderByDescending(a => a.Pos.Y).First())
            .ToList();

        if (groupedByX.Count == 0) return;

        var shooter = groupedByX[_rng.NextInt(0, groupedByX.Count)];
        var bpos2 = new GridPoint(shooter.Pos.X, shooter.Pos.Y + 1);
        if (!State.InBounds(bpos2)) return;

        State.Entities.Add(new Entity(AllocateId(), EntityKind.AlienBullet, bpos2)
        {
            Damage = 1,
            Hp = 1
        });

        // Lose condition: aliens too low
        var lowest = aliens.Max(a => a.Pos.Y);
        if (lowest >= State.Player.Pos.Y)
        {
            State.Player.Hp = 0;
        }
    }

    private void HandleBullets()
    {
        foreach (var b in State.Bullets)
        {
            var dy = b.Kind == EntityKind.PlayerBullet ? -1 : 1;
            b.Pos = new GridPoint(b.Pos.X, b.Pos.Y + dy);
        }

        // despawn out of bounds
        State.Entities.RemoveAll(e => (e.Kind is EntityKind.PlayerBullet or EntityKind.AlienBullet) && !State.InBounds(e.Pos));
    }

    private void ResolveCollisionsAndCleanup()
    {
        // player bullets vs aliens
        var bullets = State.Entities.Where(e => e.Kind == EntityKind.PlayerBullet).ToList();
        var aliens = State.Entities.Where(e => e.Kind == EntityKind.Alien).ToList();

        var alienByPos = aliens
            .GroupBy(a => a.Pos)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var b in bullets)
        {
            if (!alienByPos.TryGetValue(b.Pos, out var hitList)) continue;
            var target = hitList[0];
            target.Hp -= b.Damage;
            b.Hp = 0;

            if (target.Hp <= 0)
            {
                State.Run.Score += 10;
                State.Run.Credits += 0; // reserved
            }
        }

        // alien bullets vs player
        foreach (var b in State.Entities.Where(e => e.Kind == EntityKind.AlienBullet).ToList())
        {
            if (b.Pos == State.Player.Pos)
            {
                State.Player.Hp -= b.Damage;
                b.Hp = 0;
            }
        }

        // cleanup dead entities (leave player)
        State.Entities.RemoveAll(e => e.Kind != EntityKind.Player && e.Hp <= 0);
    }
}
