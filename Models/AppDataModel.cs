using System.Collections.ObjectModel;

namespace NextBand.Models;

public sealed class AppDataModel
{
    public UserModel User { get; set; } = new();
    public BandDeviceModel Band { get; set; } = new();
    public EmergencyProfileModel EmergencyProfile { get; set; } = new();
    public ObservableCollection<ConnectionModel> Connections { get; set; } =
    [
        new() { Name = "Ana Silva", UserName = "anasilva", ConnectedAgo = "2 min atras", IsNew = true },
        new() { Name = "Carlos Lima", UserName = "carloslima", ConnectedAgo = "15 min atras", IsNew = true },
        new() { Name = "Beatriz Souza", UserName = "beasouza", ConnectedAgo = "1h atras" },
        new() { Name = "Diego Santos", UserName = "diegosantos", ConnectedAgo = "3h atras" },
        new() { Name = "Fernanda Costa", UserName = "fecosta", ConnectedAgo = "1 dia atras" },
        new() { Name = "Gabriel Rocha", UserName = "gabrocha", ConnectedAgo = "2 dias atras" }
    ];
}
