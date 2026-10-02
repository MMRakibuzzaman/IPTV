using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using IPTV.ViewModels;

namespace IPTV.Views;

public partial class DesktopPlayerView : UserControl
{
    public DesktopPlayerView()
    {
        InitializeComponent();
        Focusable = true;
        
        // Intercept keys during the tunneling phase before children (like Buttons or Focus Navigation) swallow them
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
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
            else if (e.Key == Key.Left)
            {
                vm.PlayPreviousChannelCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                vm.PlayNextChannelCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (vm.Volume < 1.0f) vm.Volume = System.Math.Min(1.0f, vm.Volume + 0.1f);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (vm.Volume > 0.0f) vm.Volume = System.Math.Max(0.0f, vm.Volume - 0.1f);
                e.Handled = true;
            }
            else if (e.Key == Key.M)
            {
                vm.Volume = vm.Volume > 0 ? 0 : 1;
                e.Handled = true;
            }
        }
    }

    private void OnQualityItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Models.StreamQuality quality && DataContext is PlayerViewModel vm)
        {
            vm.SelectQuality(quality);
        }
    }
}


