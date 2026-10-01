using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        
        // Intercept keys during the tunneling phase before children (like Buttons or Focus Navigation) swallow them
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is PlayerViewModel vm)
            {
                vm.PropertyChanged -= Vm_PropertyChanged;
                vm.PropertyChanged += Vm_PropertyChanged;
                UpdateGridRows(vm);
            }
            CreateVideoSurface();
        };
        Loaded += (_, _) =>
        {
            if (DataContext is PlayerViewModel vm)
            {
                UpdateGridRows(vm);
            }
            CreateVideoSurface();
        };
        CreateVideoSurface();
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.IsPortraitMode) && DataContext is PlayerViewModel vm)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateGridRows(vm));
        }
    }

    private void UpdateGridRows(PlayerViewModel vm)
    {
        RootGrid.RowDefinitions = RowDefinitions.Parse(vm.GridRowDefinitions);
    }

    /// <summary>
    /// Creates the platform-appropriate video surface.
    /// LibVLCSharp.Avalonia.VideoView only supports desktop (Windows/Linux/macOS).
    /// On Android/iOS it uses platform-specific NativeControlHost embedding.
    /// </summary>
    private void CreateVideoSurface()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            var platformService = IPTV.App.Services?.GetService(typeof(IPTV.Services.Platform.IPlatformService)) as IPTV.Services.Platform.IPlatformService;
            var vm = DataContext as PlayerViewModel;
            
            if (platformService != null && vm != null && vm.MediaPlayer != null)
            {
                // If a NativeControlHost is already set, do not re-create
                if (VideoContainer.Content is NativeControlHost)
                {
                    return;
                }

                var view = platformService.CreateVideoView(vm.MediaPlayer, () =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.ToggleControls());
                });
                if (view != null)
                {
                    view.HorizontalAlignment = HorizontalAlignment.Stretch;
                    view.VerticalAlignment = VerticalAlignment.Stretch;
                    VideoContainer.Content = view;
                    return;
                }
            }

            // Only show Audio Mode fallback if DataContext has been initialized but no native video view was provided
            if (vm != null && VideoContainer.Content == null)
            {
                var panel = new Panel
                {
                    Background = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                var icon = new TextBlock
                {
                    Text = "??",
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

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            if (e.Key == Key.Escape && vm.IsFullscreen)
            {
                vm.ToggleOrientationCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F11 || e.Key == Key.F)
            {
                vm.ToggleOrientationCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                vm.TogglePlayPauseCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                vm.Volume = Math.Min(1.0, vm.Volume + 0.05);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                vm.Volume = Math.Max(0.0, vm.Volume - 0.05);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                _ = vm.PlayPreviousChannelAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                _ = vm.PlayNextChannelAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.M)
            {
                vm.Volume = vm.Volume > 0 ? 0.0 : 1.0;
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
            PortraitQualityButton?.Flyout?.Hide();
        }
    }
}


