using CommunityToolkit.Mvvm.ComponentModel;

namespace IPTV.Models;

public partial class StreamQuality : ObservableObject
{
    [ObservableProperty]
    private string _name = "Auto";
    public string Url { get; set; } = string.Empty;
    public int Bandwidth { get; set; }

    [ObservableProperty]
    private bool _isSelected;
}
