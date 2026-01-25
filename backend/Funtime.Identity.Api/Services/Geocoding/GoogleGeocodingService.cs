using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Funtime.Identity.Api.Services.Geocoding;

/// <summary>
/// Google Maps Geocoding API implementation
/// </summary>
public class GoogleGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly GeocodingOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleGeocodingService> _logger;
    private const string BaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";

    public bool IsEnabled => _options.Enabled &&
                             _options.Provider.Equals("google", StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrEmpty(_options.Google.ApiKey);

    public string ProviderName => "google";

    public GoogleGeocodingService(
        HttpClient httpClient,
        IOptions<GeocodingOptions> options,
        IMemoryCache cache,
        ILogger<GoogleGeocodingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeocodingResult> GeocodeAsync(GeocodingRequest request)
    {
        if (!IsEnabled)
            return GeocodingResult.Failed("Google geocoding not enabled", ProviderName);

        var address = request.ToAddressString();
        var cacheKey = $"geocode_google_{address.ToLowerInvariant().GetHashCode()}";

        // Check cache first
        if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out GeocodingResult? cached) && cached != null)
        {
            _logger.LogDebug("Geocoding cache hit for {Address}", address);
            return cached;
        }

        try
        {
            var url = $"{BaseUrl}?address={Uri.EscapeDataString(address)}&key={_options.Google.ApiKey}";

            if (!string.IsNullOrEmpty(_options.Google.Region))
                url += $"&region={_options.Google.Region}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();

            if (status != "OK")
            {
                var error = status == "ZERO_RESULTS" ? "No results found" : $"API returned: {status}";
                _logger.LogWarning("Google geocoding failed for {Address}: {Status}", address, status);
                return GeocodingResult.Failed(error, ProviderName);
            }

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0)
                return GeocodingResult.Failed("No results found", ProviderName);

            var firstResult = results[0];
            var location = firstResult.GetProperty("geometry").GetProperty("location");
            var lat = location.GetProperty("lat").GetDecimal();
            var lng = location.GetProperty("lng").GetDecimal();
            var formattedAddress = firstResult.GetProperty("formatted_address").GetString();

            var result = GeocodingResult.Succeeded(lat, lng, ProviderName, formattedAddress);

            // Cache successful result
            if (_options.EnableCaching)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheMinutes));
            }

            _logger.LogInformation("Geocoded {Address} to ({Lat}, {Lng}) via Google", address, lat, lng);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google geocoding error for {Address}", address);
            return GeocodingResult.Failed($"Geocoding error: {ex.Message}", ProviderName);
        }
    }
}
