using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Funtime.Identity.Api.Services.Geocoding;

/// <summary>
/// Azure Maps Geocoding implementation
/// </summary>
public class AzureMapsGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly GeocodingOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AzureMapsGeocodingService> _logger;
    private const string BaseUrl = "https://atlas.microsoft.com/search/address/json";

    public bool IsEnabled => _options.Enabled &&
                             _options.Provider.Equals("azure", StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrEmpty(_options.AzureMaps.SubscriptionKey);

    public string ProviderName => "azure";

    public AzureMapsGeocodingService(
        HttpClient httpClient,
        IOptions<GeocodingOptions> options,
        IMemoryCache cache,
        ILogger<AzureMapsGeocodingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeocodingResult> GeocodeAsync(GeocodingRequest request)
    {
        if (!IsEnabled)
            return GeocodingResult.Failed("Azure Maps geocoding not enabled", ProviderName);

        var address = request.ToAddressString();
        var cacheKey = $"geocode_azure_{address.ToLowerInvariant().GetHashCode()}";

        // Check cache first
        if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out GeocodingResult? cached) && cached != null)
        {
            _logger.LogDebug("Geocoding cache hit for {Address}", address);
            return cached;
        }

        try
        {
            var url = $"{BaseUrl}?api-version=1.0&query={Uri.EscapeDataString(address)}&subscription-key={_options.AzureMaps.SubscriptionKey}";

            // Add country filter if available
            if (!string.IsNullOrEmpty(request.CountryCode))
            {
                url += $"&countrySet={request.CountryCode.ToUpper()}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                _logger.LogWarning("Azure Maps: No results for {Address}", address);
                return GeocodingResult.Failed("No results found", ProviderName);
            }

            var firstResult = results[0];
            var position = firstResult.GetProperty("position");
            var lat = position.GetProperty("lat").GetDecimal();
            var lng = position.GetProperty("lon").GetDecimal();

            var formattedAddress = firstResult.TryGetProperty("address", out var addressProp) &&
                                   addressProp.TryGetProperty("freeformAddress", out var freeform)
                ? freeform.GetString()
                : null;

            var result = GeocodingResult.Succeeded(lat, lng, ProviderName, formattedAddress);

            // Cache successful result
            if (_options.EnableCaching)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheMinutes));
            }

            _logger.LogInformation("Geocoded {Address} to ({Lat}, {Lng}) via Azure Maps", address, lat, lng);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Maps geocoding error for {Address}", address);
            return GeocodingResult.Failed($"Geocoding error: {ex.Message}", ProviderName);
        }
    }
}
