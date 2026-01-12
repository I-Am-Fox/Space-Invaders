using System;
using System.Windows;
using System.Windows.Controls;
using SpaceInvaders.Core.Upgrades;
using SpaceInvaders.Wpf.Helpers;

namespace SpaceInvaders.Wpf.Views;

public partial class SettingsPage
{
    private readonly ShellWindow _shell;
    private bool _isInitializing;

    public SettingsPage(ShellWindow shell)
    {
        // IMPORTANT: event handlers can fire during InitializeComponent (XAML sets Slider values).
        // We must have _shell set before that happens.
        _shell = shell;

        _isInitializing = true;
        try
        {
            InitializeComponent();

            var meta = _shell.Session.Meta;

            // Audio
            MuteCheck.IsChecked = meta.IsMuted;
            MusicSlider.Value = meta.MusicVolume;
            SfxSlider.Value = meta.SfxVolume;

            // UI scaling
            UiAutoCheck.IsChecked = meta.UiScaleAuto;
            UiScaleSlider.Value = meta.UiScale;
            UiScaleSlider.IsEnabled = !meta.UiScaleAuto;
            UpdateUiScaleLabel(meta);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public void Back_Click(object sender, RoutedEventArgs e)
    {
        _shell.NavigateToMainMenu(this);
    }

    public void AudioChanged_Click(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        ApplyAndSaveAudio();
    }

    public void MusicChanged_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        ApplyAndSaveAudio();
    }

    public void SfxChanged_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        ApplyAndSaveAudio();
    }

    public void TestSfx_Click(object sender, RoutedEventArgs e)
    {
        ApplyAndSaveAudio();
        _shell.Audio.PlaySfx(System.IO.Path.Combine(AppContext.BaseDirectory, "Sounds", "laser.ogg"));
    }

    public void UiScaleChanged_Click(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        ApplyAndSaveUiScale();
    }

    public void UiScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        // NOTE: This event can fire during InitializeComponent before named elements are fully wired.
        // Using e.NewValue avoids touching UiScaleSlider when it may still be null.
        ApplyAndSaveUiScale(manualScaleOverride: e.NewValue);
    }

    private void ApplyAndSaveUiScale(double? manualScaleOverride = null)
    {
        if (_shell.Session?.Meta is not { } meta) return;

        meta.UiScaleAuto = UiAutoCheck?.IsChecked == true;

        if (manualScaleOverride is not null)
            meta.UiScale = (float)Math.Clamp(manualScaleOverride.Value, 0.75, 1.75);
        else if (UiScaleSlider is not null)
            meta.UiScale = (float)Math.Clamp(UiScaleSlider.Value, 0.75, 1.75);

        if (UiScaleSlider is not null)
            UiScaleSlider.IsEnabled = !meta.UiScaleAuto;

        if (UiScaleLabel is not null)
            UpdateUiScaleLabel(meta);

        // Always re-apply scale so toggling Auto responds immediately.
        _shell.ApplyUiScale();
        _shell.SaveProfile();
    }

    private void ApplyAndSaveAudio()
    {
        var meta = _shell.Session.Meta;

        meta.IsMuted = MuteCheck.IsChecked == true;
        meta.MusicVolume = (float)Math.Clamp(MusicSlider.Value, 0.0, 1.0);
        meta.SfxVolume = (float)Math.Clamp(SfxSlider.Value, 0.0, 1.0);

        // Apply live.
        _shell.Audio.IsMuted = meta.IsMuted;
        _shell.Audio.MusicVolume = meta.MusicVolume;
        _shell.Audio.SfxVolume = meta.SfxVolume;

        _shell.SaveProfile();
    }

    private void UpdateUiScaleLabel(MetaProgression meta)
    {
        if (meta.UiScaleAuto)
        {
            UiScaleLabel.Text = "Using auto scale based on your display (1080p/1440p/4K + DPI).";
        }
        else
        {
            UiScaleLabel.Text = $"Manual scale: {meta.UiScale:0.00}x";
        }
    }
}
