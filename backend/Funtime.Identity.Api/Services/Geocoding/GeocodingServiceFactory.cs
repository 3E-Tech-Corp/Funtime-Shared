using Microsoft.Extensions.Options;

namespace Funtime.Identity.Api.Services.Geocoding;

/// <summary>
/// Factory that selects the appropriate geocoding provider based on configuration
/// </summary>
public class GeocodingServiceFactory : IGeocodingService
{
    private readonly GeocodingOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GeocodingServiceFactory> _logger;

    public bool IsEnabled => _options.Enabled && !string.IsNullOrEmpty(_options.Provider) && _options.Provider != "none";

    public string ProviderName => _options.Provider;

    public GeocodingServiceFactory(
        IOptions<GeocodingOptions> options,
        IServiceProvider serviceProvider,
        ILogger<GeocodingServiceFactory> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<GeocodingResult> GeocodeAsync(GeocodingRequest request)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Geocoding is disabled");
            return GeocodingResult.Failed("Geocoding is disabled", "none");
        }

        var provider = GetProvider();
        if (provider == null || !provider.IsEnabled)
        {
            _logger.LogWarning("Geocoding provider '{Provider}' is not configured correctly", _options.Provider);
            return GeocodingResult.Failed($"Provider '{_options.Provider}' is not configured", _options.Provider);
        }

        return await provider.GeocodeAsync(request);
    }

    private IGeocodingService? GetProvider()
    {
        return _options.Provider.ToLower() switch
        {
            "google" => _serviceProvider.GetService<GoogleGeocodingService>(),
            "nominatim" => _serviceProvider.GetService<NominatimGeocodingService>(),
            "azure" => _serviceProvider.GetService<AzureMapsGeocodingService>(),
            _ => null
        };
    }
}
