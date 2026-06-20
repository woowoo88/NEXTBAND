using System.Text.RegularExpressions;

namespace NextBand.Services;

public sealed class ValidationService
{
    public bool IsEmail(string value)
    {
        return Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public bool IsBrazilianPhone(string value)
    {
        return Regex.IsMatch(value.Trim(), @"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$");
    }

    public bool IsBloodType(string value)
    {
        return new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }
            .Contains(value.Trim().ToUpperInvariant());
    }
}
