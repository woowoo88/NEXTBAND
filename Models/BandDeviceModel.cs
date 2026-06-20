namespace NextBand.Models;

public sealed class BandDeviceModel : ObservableModel
{
    private string _deviceName = "ESP32";
    private bool _isConnected;
    private bool _ledEnabled = true;
    private string _ledHexColor = "#3B82F6";
    private string _oledText = "@lucasmendes";
    private string _quickLinkType = "Instagram";
    private string _quickLink = "https://instagram.com/lucasmendes";
    private bool _childMode;
    private bool _notificationsEnabled = true;
    private bool _locationEnabled;

    public string DeviceName { get => _deviceName; set => SetProperty(ref _deviceName, value); }
    public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }
    public bool LedEnabled { get => _ledEnabled; set => SetProperty(ref _ledEnabled, value); }
    public string LedHexColor { get => _ledHexColor; set => SetProperty(ref _ledHexColor, value); }
    public string OledText { get => _oledText; set => SetProperty(ref _oledText, value.Length > 20 ? value[..20] : value); }
    public string QuickLinkType { get => _quickLinkType; set => SetProperty(ref _quickLinkType, value); }
    public string QuickLink { get => _quickLink; set => SetProperty(ref _quickLink, value); }
    public bool ChildMode { get => _childMode; set => SetProperty(ref _childMode, value); }
    public bool NotificationsEnabled { get => _notificationsEnabled; set => SetProperty(ref _notificationsEnabled, value); }
    public bool LocationEnabled { get => _locationEnabled; set => SetProperty(ref _locationEnabled, value); }
}
