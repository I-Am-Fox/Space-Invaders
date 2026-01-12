using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SpaceInvaders.Core.Engine;
using SpaceInvaders.Core.Model;
using SpaceInvaders.Core.Upgrades;
using SpaceInvaders.Wpf.Helpers;

namespace SpaceInvaders.Wpf.Views;

public partial class GamePage
{
    private readonly ShellWindow _shell;

    private readonly DispatcherTimer _timer;
    private DateTime _lastSim = DateTime.UtcNow;

    private bool _leftDown;
    private bool _rightDown;
    private bool _shootDown;

    private const double Cell = 20.0;
    private readonly Dictionary<int, FrameworkElement> _elementByEntityId = new();
    private readonly Dictionary<string, BitmapImage> _spriteCache = new(StringComparer.OrdinalIgnoreCase);

    // Use the shared app audio service (NAudio/Vorbis) for reliable .ogg playback.
    private AudioFacade _audio;

    private GameConfig _config = GameConfig.Default with { Width = 34, Height = 22, TickSeconds = 0.05 };

    public GamePage(ShellWindow shell)
    {
        InitializeComponent();
        _shell = shell;
        _audio = new AudioFacade(shell);

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += (_, _) => Tick();

        Focusable = true;
    }

    public void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartRun();
        Focus();
        _audio.PlayMusicLoop("music.ogg");
        _timer.Start();
    }

    private void StartRun()
    {
        _shell.Session.StartNewRun(_config with { Seed = Environment.TickCount });
        ResetVisuals();
    }

    private Game Game => _shell.Session.CurrentGame ?? throw new InvalidOperationException("No active game");

    private void ResetVisuals()
    {
        _elementByEntityId.Clear();
        GameCanvas.Children.Clear();

        GameCanvas.Width = _config.Width * Cell;
        GameCanvas.Height = _config.Height * Cell;

        UpgradeOverlay.Visibility = Visibility.Collapsed;
        GameOverOverlay.Visibility = Visibility.Collapsed;
        ExitRunOverlay.Visibility = Visibility.Collapsed;

        _leftDown = _rightDown = _shootDown = false;
        _lastSim = DateTime.UtcNow;
    }

    private bool IsExitOverlayOpen => ExitRunOverlay.Visibility == Visibility.Visible;

    private void ShowExitOverlay()
    {
        if (_shell.Session.CurrentGame is not { } game) return;
        if (game.State.IsGameOver) return;

        // Pause sim and clear held inputs so we don't "drift" when resuming.
        _timer.Stop();
        _leftDown = _rightDown = _shootDown = false;

        ExitRunText.Text =
            $"You have {game.State.Run.Credits} run coins.\n\n" +
            "Cash out will bank them into your permanent coins.\n" +
            "Abandon will lose them.";

        ExitRunOverlay.Visibility = Visibility.Visible;
    }

    private void HideExitOverlay(bool resumeTimer)
    {
        ExitRunOverlay.Visibility = Visibility.Collapsed;

        if (resumeTimer)
        {
            _lastSim = DateTime.UtcNow;
            _timer.Start();
        }
    }

    public void OnCashOut(object sender, RoutedEventArgs e)
    {
        if (_shell.Session.CurrentGame is { } activeGame && !activeGame.State.IsGameOver)
        {
            _shell.Session.EndRunAndBankCoins();
            _shell.SaveProfile();
        }

        _shell.ClearRunSave();

        HideExitOverlay(resumeTimer: false);
        _shell.NavigateToMainMenu(this);
    }

    public void OnAbandon(object sender, RoutedEventArgs e)
    {
        if (_shell.Session.CurrentGame is { } activeGame && !activeGame.State.IsGameOver)
            _shell.Session.AbandonRun();

        _shell.ClearRunSave();

        HideExitOverlay(resumeTimer: false);
        _shell.NavigateToMainMenu(this);
    }

    public void OnCancelExit(object sender, RoutedEventArgs e)
    {
        HideExitOverlay(resumeTimer: true);
        Focus();
    }

    private void Tick()
    {
        if (_shell.Session.CurrentGame is null) return;

        var now = DateTime.UtcNow;
        var dt = now - _lastSim;
        var tickSeconds = _config.TickSeconds <= 0 ? 0.05 : _config.TickSeconds;

        while (dt.TotalSeconds >= tickSeconds)
        {
            StepSimulation();
            _lastSim += TimeSpan.FromSeconds(tickSeconds);
            dt = now - _lastSim;
        }

        RenderAll();
    }

    private void StepSimulation()
    {
        var game = Game;

        if (game.State.IsGameOver) return;
        if (game.PendingUpgrades is not null) return;

        var cmd = GameCommand.None;
        if (_leftDown && !_rightDown) cmd = GameCommand.MoveLeft;
        else if (_rightDown && !_leftDown) cmd = GameCommand.MoveRight;
        else if (_shootDown) cmd = GameCommand.Shoot;

        var beforeScore = game.State.Run.Score;
        var beforeHp = game.State.Player.Hp;
        var beforeBulletCount = game.State.Entities.Count(e => e.Kind == EntityKind.PlayerBullet);

        game.Step(cmd);

        var afterBulletCount = game.State.Entities.Count(e => e.Kind == EntityKind.PlayerBullet);
        if (afterBulletCount > beforeBulletCount)
            _audio.PlaySfx("laser.ogg");

        if (game.State.Run.Score > beforeScore)
            _audio.PlaySfx("explosion.ogg");

        if (game.State.Player.Hp < beforeHp)
            _audio.PlaySfx("explosion.ogg");

        if (game.PendingUpgrades is not null)
            ShowUpgradeOverlay(game.PendingUpgrades);

        if (game.State.IsGameOver)
        {
            // Bank coins immediately when run ends.
            _shell.Session.EndRunAndBankCoins();
            _shell.SaveProfile();
            _shell.ClearRunSave();
            ShowGameOverOverlay(beforeScore);
        }
    }

    private void ShowUpgradeOverlay(IReadOnlyList<Upgrade> upgrades)
    {
        UpgradeOverlay.Visibility = Visibility.Visible;

        void SetButton(Button btn, int idx)
        {
            if (idx >= upgrades.Count)
            {
                btn.Visibility = Visibility.Collapsed;
                return;
            }

            btn.Visibility = Visibility.Visible;
            var u = upgrades[idx];

            btn.Content = $"{idx + 1}. {u.Name}\n[{u.Rarity}] {u.Description}";

            var brush = u.Rarity switch
            {
                UpgradeRarity.Common => Brushes.LightGray,
                UpgradeRarity.Rare => new SolidColorBrush(Color.FromRgb(0x4D, 0xA3, 0xFF)),
                UpgradeRarity.VeryRare => new SolidColorBrush(Color.FromRgb(0xB2, 0x63, 0xFF)),
                UpgradeRarity.Legendary => new SolidColorBrush(Color.FromRgb(0xFF, 0xD1, 0x00)),
                UpgradeRarity.Mythic => new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x3B)),
                _ => Brushes.White
            };

            btn.Foreground = brush;
            btn.BorderBrush = brush;
        }

        SetButton(UpgradeBtn1, 0);
        SetButton(UpgradeBtn2, 1);
        SetButton(UpgradeBtn3, 2);
        SetButton(UpgradeBtn4, 3);
        SetButton(UpgradeBtn5, 4);
    }

    private void HideUpgradeOverlay() => UpgradeOverlay.Visibility = Visibility.Collapsed;

    private void ShowGameOverOverlay(int finalScore)
    {
        GameOverOverlay.Visibility = Visibility.Visible;
        GameOverText.Text = $"Run ended\nScore: {finalScore}\nCoins banked.\nPress R to start a new run";
    }

    public void OnMenu(object sender, RoutedEventArgs e)
    {
        // If a run is active, show an in-game overlay instead of a pop-up.
        if (_shell.Session.CurrentGame is { } activeGame && !activeGame.State.IsGameOver)
        {
            // If upgrade selection is up, don't stack another modal on top.
            if (activeGame.PendingUpgrades is not null) return;

            if (IsExitOverlayOpen)
                HideExitOverlay(resumeTimer: true);
            else
                ShowExitOverlay();

            return;
        }

        _timer.Stop();
        _shell.NavigateToMainMenu(this);
    }

    public void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_shell.Session.CurrentGame is null)
        {
            if (e.Key == Key.R) StartRun();
            if (e.Key == Key.Escape) OnMenu(this, new RoutedEventArgs());
            return;
        }

        var game = Game;

        if (IsExitOverlayOpen)
        {
            if (e.Key == Key.Escape)
                HideExitOverlay(resumeTimer: true);

            return;
        }

        if (game.State.IsGameOver)
        {
            if (e.Key == Key.R) StartRun();
            return;
        }

        if (game.PendingUpgrades is { } pending)
        {
            var pick = e.Key switch
            {
                Key.D1 or Key.NumPad1 => 0,
                Key.D2 or Key.NumPad2 => 1,
                Key.D3 or Key.NumPad3 => 2,
                Key.D4 or Key.NumPad4 => 3,
                Key.D5 or Key.NumPad5 => 4,
                _ => -1
            };

            if (pick >= 0 && pick < pending.Count)
            {
                game.Step(GameCommand.None, pending[pick].Id);
                HideUpgradeOverlay();
            }

            return;
        }

        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                _leftDown = true;
                break;
            case Key.Right:
            case Key.D:
                _rightDown = true;
                break;
            case Key.Space:
                _shootDown = true;
                break;
            case Key.P:
                game.Step(GameCommand.Pause);
                break;
            case Key.Escape:
                OnMenu(this, new RoutedEventArgs());
                break;
        }
    }

    public void OnKeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                _leftDown = false;
                break;
            case Key.Right:
            case Key.D:
                _rightDown = false;
                break;
            case Key.Space:
                _shootDown = false;
                break;
        }
    }

    public void OnUpgrade1(object sender, RoutedEventArgs e) => PickUpgradeByIndex(0);
    public void OnUpgrade2(object sender, RoutedEventArgs e) => PickUpgradeByIndex(1);
    public void OnUpgrade3(object sender, RoutedEventArgs e) => PickUpgradeByIndex(2);
    public void OnUpgrade4(object sender, RoutedEventArgs e) => PickUpgradeByIndex(3);
    public void OnUpgrade5(object sender, RoutedEventArgs e) => PickUpgradeByIndex(4);

    private void PickUpgradeByIndex(int idx)
    {
        var game = Game;
        if (game.PendingUpgrades is not { } pending) return;
        if (idx < 0 || idx >= pending.Count) return;

        game.Step(GameCommand.None, pending[idx].Id);
        HideUpgradeOverlay();
    }

    private void RenderAll()
    {
        if (_shell.Session.CurrentGame is null) return;

        var state = Game.State;

        WaveBoxText.Text = state.Run.Wave.ToString();

        HudText.Text = $"Score {state.Run.Score}   HP {state.Player.Hp}/{state.Run.PlayerMaxHp}   Coins {state.Run.Credits}";
        StatusText.Text = state.IsPaused ? "Paused" : (state.StatusLine ?? string.Empty);

        var liveIds = new HashSet<int>(state.Entities.Select(e => e.Id));

        foreach (var stale in _elementByEntityId.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            GameCanvas.Children.Remove(_elementByEntityId[stale]);
            _elementByEntityId.Remove(stale);
        }

        foreach (var e in state.Entities)
        {
            if (!_elementByEntityId.TryGetValue(e.Id, out var element))
            {
                element = CreateElement(e);
                _elementByEntityId[e.Id] = element;
                GameCanvas.Children.Add(element);
            }

            UpdateElement(e, element);
        }

        if (Game.PendingUpgrades is not null)
            UpgradeOverlay.Visibility = Visibility.Visible;
    }

    private FrameworkElement CreateElement(Entity e)
    {
        var sprite = TryGetSpriteUri(e);
        if (sprite is not null)
        {
            return new Image
            {
                Width = Cell,
                Height = Cell,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                Source = LoadSprite(sprite)
            };
        }

        return CreateFallbackShape(e);
    }

    private static Shape CreateFallbackShape(Entity e)
    {
        return e.Kind switch
        {
            EntityKind.Player => new Polygon
            {
                Points = new PointCollection { new Point(Cell * 0.5, 0), new Point(0, Cell), new Point(Cell, Cell) },
                Fill = Brushes.Cyan,
                Stroke = Brushes.White,
                StrokeThickness = 1
            },
            EntityKind.Alien => new Ellipse
            {
                Width = Cell * 0.8,
                Height = Cell * 0.8,
                Fill = Brushes.LimeGreen,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 2
            },
            EntityKind.PlayerBullet => new Rectangle
            {
                Width = Cell * 0.2,
                Height = Cell * 0.8,
                Fill = Brushes.Gold
            },
            EntityKind.AlienBullet => new Rectangle
            {
                Width = Cell * 0.2,
                Height = Cell * 0.8,
                Fill = Brushes.OrangeRed
            },
            _ => new Rectangle
            {
                Width = Cell * 0.8,
                Height = Cell * 0.8,
                Fill = Brushes.Violet
            }
        };
    }

    private void UpdateElement(Entity e, FrameworkElement element)
    {
        var x = e.Pos.X * Cell + (Cell - element.Width) / 2;
        var y = e.Pos.Y * Cell + (Cell - element.Height) / 2;

        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);

        element.Opacity = e.Kind == EntityKind.Alien ? (e.Hp >= 2 ? 0.95 : 0.8) : 1.0;
    }

    private Uri? TryGetSpriteUri(Entity e)
    {
        return e.Kind switch
        {
            EntityKind.Player => new Uri("pack://application:,,,/Graphics/spaceship.png"),
            EntityKind.Alien => new Uri($"pack://application:,,,/Graphics/alien_{(e.Id % 3) + 1}.png"),
            _ => null
        };
    }

    private BitmapImage LoadSprite(Uri uri)
    {
        var key = uri.ToString();
        if (_spriteCache.TryGetValue(key, out var cached))
            return cached;

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = uri;
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();

        _spriteCache[key] = bmp;
        return bmp;
    }

    private sealed class AudioFacade
    {
        private readonly ShellWindow _shell;

        public AudioFacade(ShellWindow shell)
        {
            _shell = shell;
        }

        public void PlaySfx(string fileName)
        {
            SyncFromProfile();
            _shell.Audio.PlaySfx(ResolveSoundPath(fileName));
        }

        public void PlayMusicLoop(string fileName)
        {
            SyncFromProfile();
            _shell.Audio.PlayMusicLoop(ResolveSoundPath(fileName));
        }

        private void SyncFromProfile()
        {
            // Allow Settings page to change these live.
            var meta = _shell.Session.Meta;
            _shell.Audio.IsMuted = meta.IsMuted;
            _shell.Audio.MusicVolume = meta.MusicVolume;
            _shell.Audio.SfxVolume = meta.SfxVolume;
        }

        private static string ResolveSoundPath(string fileName)
        {
            // Copied to output by the WPF csproj.
            return System.IO.Path.Combine(AppContext.BaseDirectory, "Sounds", fileName);
        }
    }
}

