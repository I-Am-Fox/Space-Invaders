using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SpaceInvaders.Wpf.Helpers;

/// <summary>
/// Helper class for smooth page transitions with fade animations
/// </summary>
public static class PageTransitionHelper
{
    /// <summary>
    /// Navigate to a new page with a fade out/in transition
    /// </summary>
    /// <param name="frame">The frame to navigate</param>
    /// <param name="currentPage">The current page to fade out</param>
    /// <param name="newPage">The new page to navigate to</param>
    /// <param name="fadeOutDuration">Duration of fade out in seconds (default: 0.15)</param>
    /// <param name="fadeInDuration">Duration of fade in in seconds (default: 0.2)</param>
    public static void NavigateWithFade(
        Frame frame, 
        Page currentPage, 
        Page newPage, 
        double fadeOutDuration = 0.15,
        double fadeInDuration = 0.2)
    {
        // Create fade out animation
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromSeconds(fadeOutDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        // When fade out completes, navigate and fade in
        fadeOut.Completed += (_, _) =>
        {
            // Navigate to the new page
            frame.Navigate(newPage);
            
            // Set opacity to 0 immediately
            newPage.Opacity = 0.0;
            
            // Use Dispatcher.BeginInvoke with Loaded priority to ensure the page is in the visual tree
            newPage.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Create fade in animation
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(fadeInDuration),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                newPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        };

        // Start the fade out animation
        currentPage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}

