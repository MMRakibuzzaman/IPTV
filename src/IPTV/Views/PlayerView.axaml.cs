using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using IPTV.ViewModels;

namespace IPTV.Views;

public partial class PlayerView : UserControl
{
    private Point _lastPointerPosition;

    public PlayerView()
    {
        InitializeComponent();
        Focusable = true;
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

    private void OnPlayerPointerMoved(object? sender, PointerEventArgs e)
    {
        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - _lastPointerPosition.X) > 2 ||
            Math.Abs(currentPosition.Y - _lastPointerPosition.Y) > 2)
        {
            _lastPointerPosition = currentPosition;
            if (DataContext is PlayerViewModel vm)
            {
                if (!vm.ShowControls)
                {
                    vm.ShowControls = true;
                }
                vm.ResetHideTimer();
            }
        }
    }

    private void OnPlayerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = DataContext as PlayerViewModel;
        if (vm == null) return;

        var source = e.Source as Visual;
        while (source != null && source != this)
        {
            if (source is Button || source is Slider || source is TextBox || source is ScrollViewer)
            {
                vm.ResetHideTimer();
                return;
            }
            source = source.GetVisualParent();
        }

        vm.ToggleControlsCommand.Execute(null);
    }

    private void OnVideoSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.ToggleControlsCommand.Execute(null);
            e.Handled = true;
        }
    }
}
