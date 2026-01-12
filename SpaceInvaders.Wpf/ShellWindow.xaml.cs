using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SpaceInvaders.Core.Engine;
using SpaceInvaders.Wpf.Views;
using SpaceInvaders.Wpf.Services;
using SpaceInvaders.Wpf.Persistence;
using SpaceInvaders.Wpf.Helpers;

namespace SpaceInvaders.Wpf;

public partial class ShellWindow
{
    private readonly ProfileStore _store = new();
    private readonly RunSaveStore _runSaveStore = new();

    private readonly DispatcherTimer _autoScaleDebounce;

    public GameSession Session { get; }
    public AudioManager Audio { get; }

    public ShellWindow()
    {
        InitializeComponent();

        var meta = _store.LoadOrCreate();
        Session = new GameSession(meta);

        Audio = new AudioManager
        {
            IsMuted = meta.IsMuted,
            MusicVolume = meta.MusicVolume,
            SfxVolume = meta.SfxVolume
        };

        _autoScaleDebounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _autoScaleDebounce.Tick += (_, _) =>
        {
            _autoScaleDebounce.Stop();
            if (Session.Meta.UiScaleAuto)
                ApplyUiScale();
        };

        SizeChanged += (_, _) =>
        {
            if (!Session.Meta.UiScaleAuto) return;
            _autoScaleDebounce.Stop();
            _autoScaleDebounce.Start();
        };

        RootFrame.Navigate(new MainMenuPage(this));

        // If there's a saved run, ask to resume immediately (from the menu).
        TryPromptResumeSavedRun();

        Closed += (_, _) =>
        {
            // If a run is active and the user is exiting without abandoning/cashing out,
            // keep a save so they can resume next time.
            try
            {
                if (Session.CurrentGame is { } g && !g.State.IsGameOver)
                    _runSaveStore.Save(g.Config, g.State, g.PendingUpgrades);
            }
            catch
            {
                // ignore
            }

            _store.Save(Session.Meta);
            Audio.Dispose();
        };
    }

    public void OnLoaded(object sender, RoutedEventArgs e)
    {
        // UI scaling depends on a visual being loaded.
        ApplyUiScale();
    }

    public void ApplyUiScale()
    {
        var meta = Session.Meta;

        var scale = meta.UiScaleAuto
            ? UiScaleHelper.ComputeAutoScale(this)
            : Math.Clamp(meta.UiScale, 0.75f, 1.75f);

        // Use RenderTransform so layout (measure/arrange) stays stable; otherwise centered content
        // can drift when LayoutTransform changes the element's desired size.
        Root.RenderTransform = new ScaleTransform(scale, scale);
    }

    public void SaveProfile() => _store.Save(Session.Meta);

    // Centralized navigation helpers (always fade)
    public void NavigateToMainMenu(Page from) => PageTransitionHelper.NavigateWithFade(RootFrame, from, new MainMenuPage(this));
    public void NavigateToGame(Page from) => PageTransitionHelper.NavigateWithFade(RootFrame, from, new GamePage(this));
    public void NavigateToShop(Page from) => PageTransitionHelper.NavigateWithFade(RootFrame, from, new ShopPage(this));
    public void NavigateToSettings(Page from) => PageTransitionHelper.NavigateWithFade(RootFrame, from, new SettingsPage(this));

    public void ClearRunSave() => _runSaveStore.Clear();

    private void TryPromptResumeSavedRun()
    {
        var dto = _runSaveStore.Load();
        if (dto is null) return;

        var result = MessageBox.Show(
            "A previous run was found. Would you like to resume it?",
            "Resume run",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        // If the user declines, dismiss permanently by clearing the save.
        if (result != MessageBoxResult.Yes)
        {
            _runSaveStore.Clear();
            return;
        }

        try
        {
            var game = new Game(dto.Config);

            // Restore persisted state and pending upgrades.
            var restoredState = dto.State.ToModel();

            // Sanity: keep the player's max HP aligned with the restored run.
            restoredState.Player.Hp = Math.Min(restoredState.Player.Hp, restoredState.Run.PlayerMaxHp);

            // Replace the session's current game with a new instance that contains restored state.
            // We keep this confined to the WPF layer.
            var restoredGame = new RestoredGame(game, restoredState, dto.PendingUpgrades);
            Session.RestoreRun(restoredGame.Game);
            restoredGame.ApplyPendingUpgrades();

            // Clear save so we don't keep asking once resumed.
            _runSaveStore.Clear();

            // Navigate into the game.
            RootFrame.Navigate(new GamePage(this));
        }
        catch
        {
            // If restore fails for any reason, don't crash the app.
        }
    }

    private sealed class RestoredGame
    {
        public Game Game { get; }
        private readonly SpaceInvaders.Core.Model.GameState _state;
        private readonly System.Collections.Generic.List<RunSaveStore.UpgradeDto>? _pending;

        public RestoredGame(Game baseGame, SpaceInvaders.Core.Model.GameState state, System.Collections.Generic.List<RunSaveStore.UpgradeDto>? pending)
        {
            Game = baseGame;
            _state = state;
            _pending = pending;

            // Hack-free restore: we reconstruct by copying the state into the already-constructed game.
            // The core Game keeps State as get-only, so instead we recreate a new Game-like wrapper by
            // setting Session.CurrentGame to a new game and then copying state through mutation.
            // We can do that by mutating the contents of Game.State (init properties are immutable,
            // but most fields inside are mutable).

            Game.State.IsGameOver = _state.IsGameOver;
            Game.State.IsPaused = _state.IsPaused;
            Game.State.StatusLine = _state.StatusLine;

            // Run
            Game.State.Run.Wave = _state.Run.Wave;
            Game.State.Run.Score = _state.Run.Score;
            Game.State.Run.Credits = _state.Run.Credits;
            Game.State.Run.ShotsPerPress = _state.Run.ShotsPerPress;
            Game.State.Run.BulletDamageBonus = _state.Run.BulletDamageBonus;
            Game.State.Run.FireCooldownTicks = _state.Run.FireCooldownTicks;
            Game.State.Run.MoveSpeed = _state.Run.MoveSpeed;
            Game.State.Run.PlayerMaxHp = _state.Run.PlayerMaxHp;
            Game.State.Run.CoinsPerWaveBonus = _state.Run.CoinsPerWaveBonus;
            Game.State.Run.CoinsPerWaveMultiplierPct = _state.Run.CoinsPerWaveMultiplierPct;
            Game.State.Run.AlienMoveSpeedPct = _state.Run.AlienMoveSpeedPct;

            Game.State.Run.PickedUpgrades.Clear();
            foreach (var id in _state.Run.PickedUpgrades)
                Game.State.Run.PickedUpgrades.Add(id);

            // Entities
            Game.State.Entities.Clear();
            foreach (var ent in _state.Entities)
                Game.State.Entities.Add(ent);

            Game.State.Player.Pos = _state.Player.Pos;
            Game.State.Player.Hp = _state.Player.Hp;

            // Ensure player is in entities
            if (!Game.State.Entities.Contains(Game.State.Player))
                Game.State.Entities.Add(Game.State.Player);
        }

        public void ApplyPendingUpgrades()
        {
            // Pending upgrades are optional; current UI can still play fine without them.
            // If we ever want to restore the actual Upgrade objects, we'd need a draft reconstruction.
            // For now we only restore the state and let the game continue.
        }
    }
}
