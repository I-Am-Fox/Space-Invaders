using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SpaceInvaders.Core.Engine;
using SpaceInvaders.Core.Upgrades;
using SpaceInvaders.Wpf.Views;
using SpaceInvaders.Wpf.Services;
using SpaceInvaders.Wpf.Persistence;
using SpaceInvaders.Wpf.Helpers;

namespace SpaceInvaders.Wpf;

public partial class ShellWindow
{
    private readonly ProfileStore _store = new();

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

        Closed += (_, _) =>
        {
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
}
