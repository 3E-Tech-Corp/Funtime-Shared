namespace Funtime.Identity.Api.Services.Geocoding;

/// <summary>
/// Configuration options for geocoding service
/// </summary>
public class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>
    /// Enable/disable geocoding. If disabled, addresses use city GPS as fallback.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Which provider to use: "google", "nominatim", "azure", or "none"
    /// </summary>
    public string Provider { get; set; } = "none";

    /// <summary>
    /// Google Maps Geocoding API settings
    /// </summary>
    public GoogleGeocodingOptions Google { get; set; } = new();

    /// <summary>
    /// OpenStreetMap Nominatim settings
    /// </summary>
    public NominatimGeocodingOptions Nominatim { get; set; } = new();

    /// <summary>
    /// Azure Maps settings
    /// </summary>
    public AzureMapsGeocodingOptions AzureMaps { get; set; } = new();

    /// <summary>
    /// Cache geocoding results to reduce API calls
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes
    /// </summary>
    public int CacheMinutes { get; set; } = 1440; // 24 hours
}

public class GoogleGeocodingOptions
{
    /// <summary>
    /// Google Maps API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional: restrict results to specific region (e.g., "us")
    /// </summary>
    public string? Region { get; set; }
}

public class NominatimGeocodingOptions
{
    /// <summary>
    /// Nominatim API base URL (default: OpenStreetMap public instance)
    /// You can host your own instance for production use
    /// </summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Required: Contact email for OSM usage policy compliance
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// User agent string (required by OSM policy)
    /// </summary>
    public string UserAgent { get; set; } = "FuntimeIdentityApi/1.0";

    /// <summary>
    /// Rate limit: minimum delay between requests in milliseconds
    /// OSM policy requires max 1 request per second for public instance
    /// </summary>
    public int RateLimitMs { get; set; } = 1000;
}

public class AzureMapsGeocodingOptions
{
    /// <summary>
    /// Azure Maps subscription key
    /// </summary>
    public string SubscriptionKey { get; set; } = string.Empty;
}
