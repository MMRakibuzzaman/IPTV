using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IPTV.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            topLevel.BackRequested += TopLevel_BackRequested;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            topLevel.BackRequested -= TopLevel_BackRequested;
        }
    }

    private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm && vm.CurrentView is ViewModels.PlayerViewModel playerVm)
        {
            playerVm.GoBackCommand.Execute(null);
            e.Handled = true;
        }
    }
}
