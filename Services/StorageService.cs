using System.IO;
using System.Text.Json;
using NextBand.Models;

namespace NextBand.Services;

public sealed class StorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

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
        return await JsonSerializer.DeserializeAsync<AppDataModel>(stream, SerializerOptions) ?? new AppDataModel();
    }

    public async Task<AppDataModel?> LoadByLoginAsync(string email, string password)
    {
        var data = await LoadAsync();
        var savedEmail = data.User.Email?.Trim() ?? string.Empty;
        var savedPassword = data.User.Password ?? string.Empty;

        if (!string.Equals(savedEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return savedPassword == password ? data : null;
    }

    public async Task SaveAsync(AppDataModel data)
    {
        Directory.CreateDirectory(_folder);
        await using var stream = File.Create(DataFile);
        await JsonSerializer.SerializeAsync(stream, data, SerializerOptions);
    }

    public Task DeleteLegacyLocalFileAsync()
    {
        return Task.CompletedTask;
    }
}
