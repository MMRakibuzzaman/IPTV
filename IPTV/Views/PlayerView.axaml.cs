using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using IPTV.ViewModels;

namespace IPTV.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
        Focusable = true;
        CreateVideoSurface();
    }

    /// <summary>
    /// Creates the platform-appropriate video surface.
    /// LibVLCSharp.Avalonia.VideoView only supports desktop (Windows/Linux/macOS).
    /// On Android/iOS it crashes because Attach() doesn't handle those platforms.
    /// </summary>
    private void CreateVideoSurface()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            // On mobile, LibVLCSharp.Avalonia.VideoView is not supported.
            // VLC will still play audio through its default output.
            // Show a styled panel indicating audio mode.
            var panel = new Panel
            {
                Background = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var icon = new TextBlock
            {
                Text = "🎵",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.Parse("#6366F1")),
                Opacity = 0.7
            };

            var label = new TextBlock
            {
                Text = "Audio Mode",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.Parse("#9CA3AF")),
                Margin = new Thickness(0, 60, 0, 0)
            };

            panel.Children.Add(icon);
            panel.Children.Add(label);
            VideoContainer.Content = panel;
        }
        else
        {
            // Desktop: use LibVLCSharp.Avalonia.VideoView with data binding
            var videoView = new LibVLCSharp.Avalonia.VideoView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Bind MediaPlayer property to the ViewModel
            videoView.Bind(
                LibVLCSharp.Avalonia.VideoView.MediaPlayerProperty,
                new Binding("MediaPlayer"));

            VideoContainer.Content = videoView;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (DataContext is PlayerViewModel vm)
        {
            if (e.Key == Key.Escape && vm.IsFullscreen)
            {
                vm.ToggleOrientationCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F11)
            {
                vm.ToggleOrientationCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                vm.TogglePlayPauseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnQualityItemClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is IPTV.Models.StreamQuality quality && DataContext is PlayerViewModel vm)
        {
            vm.SelectQuality(quality);
            QualityButton?.Flyout?.Hide();
        }
    }
}
