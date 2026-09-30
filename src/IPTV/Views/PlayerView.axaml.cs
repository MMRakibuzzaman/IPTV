using Avalonia.Controls;
using Avalonia.Input;
using IPTV.ViewModels;

namespace IPTV.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
    }

    private void OnVideoSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.ToggleControlsCommand.Execute(null);
        }
    }
}
