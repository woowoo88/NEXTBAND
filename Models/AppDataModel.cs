using System.Collections.ObjectModel;

namespace NextBand.Models;

public sealed class AppDataModel
{
    public UserModel User { get; set; } = new();
    public BandDeviceModel Band { get; set; } = new();
    public EmergencyProfileModel EmergencyProfile { get; set; } = new();
    public ObservableCollection<ConnectionModel> Connections { get; set; } = [];
}
