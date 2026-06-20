namespace NextBand.Services;

public sealed class NfcService
{
    public string BuildProfilePayload(string userName)
    {
        return $"nextband://profile/{userName}";
    }

    public string BuildEmergencyPayload(string childName)
    {
        return $"https://nextband.local/emergency/{Uri.EscapeDataString(childName.ToLowerInvariant().Replace(' ', '-'))}";
    }
}
