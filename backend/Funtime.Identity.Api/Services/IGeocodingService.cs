namespace Funtime.Identity.Api.Services;

/// <summary>
/// Result from a geocoding operation
/// </summary>
public class GeocodingResult
{
    public bool Success { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? FormattedAddress { get; set; }
    public string? Error { get; set; }
    public string Provider { get; set; } = "none";

    public static GeocodingResult Failed(string error, string provider = "none") => new()
    {
        Success = false,
        Error = error,
        Provider = provider
    };

    public static GeocodingResult Succeeded(decimal latitude, decimal longitude, string provider, string? formattedAddress = null) => new()
    {
        Success = true,
        Latitude = latitude,
        Longitude = longitude,
        FormattedAddress = formattedAddress,
        Provider = provider
    };
}

/// <summary>
/// Address components for geocoding
/// </summary>
public class GeocodingRequest
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Build a full address string for geocoding
    /// </summary>
    public string ToAddressString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Line1))
            parts.Add(Line1);

        if (!string.IsNullOrWhiteSpace(Line2))
            parts.Add(Line2);

        if (!string.IsNullOrWhiteSpace(City))
            parts.Add(City);

        if (!string.IsNullOrWhiteSpace(StateProvince))
            parts.Add(StateProvince);

        if (!string.IsNullOrWhiteSpace(PostalCode))
            parts.Add(PostalCode);

        if (!string.IsNullOrWhiteSpace(Country))
            parts.Add(Country);

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Service for geocoding addresses to GPS coordinates
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Whether geocoding is enabled and configured
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// The name of the active provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Geocode an address to GPS coordinates
    /// </summary>
    Task<GeocodingResult> GeocodeAsync(GeocodingRequest request);
}
