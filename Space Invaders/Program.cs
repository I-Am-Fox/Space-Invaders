using SpaceInvaders.Core.Engine;
using SpaceInvaders.Core.Model;
using SpaceInvaders.Core.Upgrades;

static GameCommand MapKey(ConsoleKeyInfo key)
{
    return key.Key switch
    {
        ConsoleKey.LeftArrow or ConsoleKey.A => GameCommand.MoveLeft,
        ConsoleKey.RightArrow or ConsoleKey.D => GameCommand.MoveRight,
        ConsoleKey.Spacebar => GameCommand.Shoot,
        ConsoleKey.P => GameCommand.Pause,
        ConsoleKey.Q or ConsoleKey.Escape => GameCommand.Quit,
        _ => GameCommand.None
    };
}

static UpgradeId? MapUpgradePick(ConsoleKeyInfo key, IReadOnlyList<Upgrade> pending)
{
    if (pending.Count == 0) return null;

    var idx = key.Key switch
    {
        ConsoleKey.D1 or ConsoleKey.NumPad1 => 0,
        ConsoleKey.D2 or ConsoleKey.NumPad2 => 1,
        ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
        ConsoleKey.D4 or ConsoleKey.NumPad4 => 3,
        ConsoleKey.D5 or ConsoleKey.NumPad5 => 4,
        _ => -1
    };

    if (idx < 0 || idx >= pending.Count) return null;
    return pending[idx].Id;
}

static void Render(GameState state)
{
    Console.SetCursorPosition(0, 0);

    // HUD
    var hud = $"Wave {state.Run.Wave}  Score {state.Run.Score}  HP {state.Player.Hp}/{state.Run.PlayerMaxHp}";
    Console.WriteLine(hud.PadRight(state.Width));

    if (!string.IsNullOrWhiteSpace(state.StatusLine))
        Console.WriteLine(state.StatusLine!.PadRight(state.Width));
    else
        Console.WriteLine(new string(' ', state.Width));

    // Build buffer
    var buffer = new char[state.Height, state.Width];
    for (var y = 0; y < state.Height; y++)
    for (var x = 0; x < state.Width; x++)
        buffer[y, x] = ' ';

    foreach (var e in state.Entities)
    {
        if (!state.InBounds(e.Pos)) continue;

        buffer[e.Pos.Y, e.Pos.X] = e.Kind switch
        {
            EntityKind.Player => 'A',
            EntityKind.Alien => 'W',
            EntityKind.PlayerBullet => '|',
            EntityKind.AlienBullet => '!',
            _ => '?'
        };
    }

    // Render playfield
    for (var y = 0; y < state.Height; y++)
    {
        for (var x = 0; x < state.Width; x++)
            Console.Write(buffer[y, x]);
        Console.WriteLine();
    }

    if (state.IsPaused)
        Console.WriteLine("[PAUSED]".PadRight(state.Width));
    else
        Console.WriteLine(new string(' ', state.Width));

    if (state.IsGameOver)
        Console.WriteLine("Press any key to exit...".PadRight(state.Width));
}

// Console setup
Console.CursorVisible = false;
try
{
    var config = GameConfig.Default with { Width = 60, Height = 20, TickSeconds = 0.05 };
    var game = new Game(config);

    Console.Clear();

    while (!game.State.IsGameOver)
    {
        Render(game.State);

        ConsoleKeyInfo? key = null;
        if (Console.KeyAvailable)
            key = Console.ReadKey(intercept: true);

        if (game.PendingUpgrades is { } pending)
        {
            // Only accept 1/2/3 while drafting
            var pick = key.HasValue ? MapUpgradePick(key.Value, pending) : null;
            game.Step(GameCommand.None, pick);
        }
        else
        {
            var cmd = key.HasValue ? MapKey(key.Value) : GameCommand.None;
            game.Step(cmd);
        }

        Thread.Sleep(TimeSpan.FromSeconds(config.TickSeconds));
    }

    Render(game.State);
    Console.ReadKey(intercept: true);
}
finally
{
    Console.CursorVisible = true;
}
