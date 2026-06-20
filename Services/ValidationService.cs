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
        var digits = Regex.Replace(value, @"\D", string.Empty);
        if (digits.StartsWith("55") && digits.Length > 2)
        {
            digits = digits[2..];
        }

        return digits.Length is 10 or 11;
    }

    public bool IsBloodType(string value)
    {
        return new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }
            .Contains(value.Trim().ToUpperInvariant());
    }
}
