using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Funtime.Identity.Api.Services.Geocoding;

/// <summary>
/// OpenStreetMap Nominatim Geocoding implementation
/// Free, but has rate limits (1 req/sec for public instance)
/// Consider hosting your own instance for production: https://nominatim.org/
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly GeocodingOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;

    public bool IsEnabled => _options.Enabled &&
                             _options.Provider.Equals("nominatim", StringComparison.OrdinalIgnoreCase);

    public string ProviderName => "nominatim";

    public NominatimGeocodingService(
        HttpClient httpClient,
        IOptions<GeocodingOptions> options,
        IMemoryCache cache,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;

        // Set required headers for OSM policy compliance
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.Nominatim.UserAgent);
        }
    }

    public async Task<GeocodingResult> GeocodeAsync(GeocodingRequest request)
    {
        if (!IsEnabled)
            return GeocodingResult.Failed("Nominatim geocoding not enabled", ProviderName);

        var address = request.ToAddressString();
        var cacheKey = $"geocode_nominatim_{address.ToLowerInvariant().GetHashCode()}";

        // Check cache first
        if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out GeocodingResult? cached) && cached != null)
        {
            _logger.LogDebug("Geocoding cache hit for {Address}", address);
            return cached;
        }

        // Rate limiting for OSM policy compliance
        await _rateLimiter.WaitAsync();
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequest;
            var delay = _options.Nominatim.RateLimitMs - (int)elapsed.TotalMilliseconds;
            if (delay > 0)
            {
                await Task.Delay(delay);
            }

            var result = await DoGeocodeAsync(request, address, cacheKey);
            _lastRequest = DateTime.UtcNow;
            return result;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private async Task<GeocodingResult> DoGeocodeAsync(GeocodingRequest request, string address, string cacheKey)
    {
        try
        {
            var baseUrl = _options.Nominatim.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

            // Add country code hint if available
            if (!string.IsNullOrEmpty(request.CountryCode))
            {
                url += $"&countrycodes={request.CountryCode.ToLower()}";
            }

            // Add contact email if configured (recommended by OSM)
            if (!string.IsNullOrEmpty(_options.Nominatim.ContactEmail))
            {
                url += $"&email={Uri.EscapeDataString(_options.Nominatim.ContactEmail)}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement;

            if (results.GetArrayLength() == 0)
            {
                _logger.LogWarning("Nominatim: No results for {Address}", address);
                return GeocodingResult.Failed("No results found", ProviderName);
            }

            var firstResult = results[0];
            var lat = decimal.Parse(firstResult.GetProperty("lat").GetString()!);
            var lng = decimal.Parse(firstResult.GetProperty("lon").GetString()!);
            var displayName = firstResult.GetProperty("display_name").GetString();

            var result = GeocodingResult.Succeeded(lat, lng, ProviderName, displayName);

            // Cache successful result
            if (_options.EnableCaching)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheMinutes));
            }

            _logger.LogInformation("Geocoded {Address} to ({Lat}, {Lng}) via Nominatim", address, lat, lng);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nominatim geocoding error for {Address}", address);
            return GeocodingResult.Failed($"Geocoding error: {ex.Message}", ProviderName);
        }
    }
}
