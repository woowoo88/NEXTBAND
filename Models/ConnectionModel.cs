namespace NextBand.Models;

public sealed class ConnectionModel
{
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ConnectedAgo { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}
