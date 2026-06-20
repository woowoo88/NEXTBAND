using System.IO;
using System.Text.Json;
using NextBand.Models;

namespace NextBand.Services;

public sealed class StorageService
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NextBand");

    private string DataFile => Path.Combine(_folder, "nextband-data.json");

    public async Task<AppDataModel> LoadAsync()
    {
        if (!File.Exists(DataFile))
        {
            return new AppDataModel();
        }

        await using var stream = File.OpenRead(DataFile);
        return await JsonSerializer.DeserializeAsync<AppDataModel>(stream) ?? new AppDataModel();
    }

    public async Task SaveAsync(AppDataModel data)
    {
        Directory.CreateDirectory(_folder);
        await using var stream = File.Create(DataFile);
        await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
    }
}
