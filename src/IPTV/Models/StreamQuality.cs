using CommunityToolkit.Mvvm.ComponentModel;

namespace IPTV.Models;

public partial class StreamQuality : ObservableObject
{
    public string Name { get; set; } = "Auto";
    public string Url { get; set; } = string.Empty;
    public int Bandwidth { get; set; }

    [ObservableProperty]
    private bool _isSelected;
}
