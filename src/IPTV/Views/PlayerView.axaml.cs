using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using IPTV.ViewModels;

namespace IPTV.Views;

public partial class PlayerView : UserControl
{
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
}
