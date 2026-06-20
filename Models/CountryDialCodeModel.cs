namespace NextBand.Models;

public sealed class CountryDialCodeModel
{
    public string IsoCode { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DialCode { get; set; } = string.Empty;
    public string DisplayName => $"{Flag} {Name} {DialCode}";
}
