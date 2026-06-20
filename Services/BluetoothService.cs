namespace NextBand.Services;

public sealed class BluetoothService
{
    public Task<IReadOnlyList<string>> ScanAsync()
    {
        IReadOnlyList<string> devices = ["ESP32", "NextBand-DevKit", "NB-OLED-RGB"];
        return Task.FromResult(devices);
    }

    public Task<bool> ConnectAsync(string deviceName)
    {
        return Task.FromResult(deviceName.Contains("ESP32", StringComparison.OrdinalIgnoreCase)
            || deviceName.Contains("NextBand", StringComparison.OrdinalIgnoreCase));
    }

    public Task SendAsync(string payload)
    {
        return Task.CompletedTask;
    }
}
