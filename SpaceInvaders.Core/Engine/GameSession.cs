using SpaceInvaders.Core.Upgrades;

namespace SpaceInvaders.Core.Engine;

/// <summary>
/// Wires together persistent meta progression and an active Game run.
/// UI can navigate between Menu/Shop/Game while reusing the same session.
/// </summary>
public sealed class GameSession
{
    public MetaProgression Meta { get; }
    public Game? CurrentGame { get; private set; }

    public GameSession(MetaProgression meta)
    {
        Meta = meta;
    }

    public void StartNewRun(GameConfig config)
    {
        var game = new Game(config);
        MetaApplication.ApplyToRun(Meta, (global::SpaceInvaders.Core.Model.RunState)game.State.Run);

        // Ensure player HP matches new max
        game.State.Player.Hp = game.State.Run.PlayerMaxHp;

        CurrentGame = game;
    }

    public void EndRunAndBankCoins()
    {
        if (CurrentGame is null) return;

        // Bank run credits as coins.
        Meta.Coins += CurrentGame.State.Run.Credits;
        CurrentGame = null;
    }

    public void AbandonRun()
    {
        CurrentGame = null;
    }

    public bool HasActiveRun => CurrentGame is not null;

    public void RestoreRun(Game game)
    {
        CurrentGame = game;
    }
}
