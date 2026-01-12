using System;
using System.Windows;
using System.Windows.Media;

namespace SpaceInvaders.Wpf.Helpers;

public static class UiScaleHelper
{
    /// <summary>
    /// Returns a UI scale multiplier based on display metrics.
    /// Uses monitor pixel height and DPI; includes a manual override via MetaProgression.
    /// </summary>
    public static double ComputeAutoScale(Window window)
    {
        // Defaults
        var dpiScale = 1.0;
        try
        {
            var src = PresentationSource.FromVisual(window);
            if (src?.CompositionTarget is not null)
                dpiScale = src.CompositionTarget.TransformToDevice.M11;
        }
        catch
        {
            // ignore
        }

        // WPF units are DIPs; multiply by dpiScale to get physical pixels.
        var pixelHeight = SystemParameters.PrimaryScreenHeight * dpiScale;

        // Map common resolutions to scale.
        var scaleFromRes = pixelHeight switch
        {
            < 1200 => 1.0,
            < 1600 => 1.15,
            < 2000 => 1.22,
            _ => 1.30
        };

        // Lightly incorporate DPI scaling. Clamp to avoid absurd sizes.
        var combined = scaleFromRes * Math.Clamp(dpiScale, 1.0, 1.5);
        return Math.Clamp(combined, 1.0, 1.6);
    }
}
