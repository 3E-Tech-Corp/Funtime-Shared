using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Models;
using System.Security.Claims;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// Geographic data endpoints for countries, provinces/states, and cities
/// </summary>
[ApiController]
[Route("geo")]
public class GeoController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<GeoController> _logger;

    public GeoController(
        IConfiguration configuration,
        ILogger<GeoController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        _logger = logger;
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    #region Countries

    /// <summary>
    /// Get all active countries
    /// </summary>
    [HttpGet("countries")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<List<CountryResponse>>> GetCountries()
    {
        using var conn = CreateConnection();
        var countries = await conn.QueryAsync<CountryResponse>(
            @"SELECT Id, Name, Code2, Code3, NumericCode, PhoneCode, SortOrder
              FROM Countries
              WHERE IsActive = 1
              ORDER BY SortOrder, Name");

        return Ok(countries.ToList());
    }

    /// <summary>
    /// Get a single country by ID
    /// </summary>
    [HttpGet("countries/{id:int}")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<CountryResponse>> GetCountry(int id)
    {
        using var conn = CreateConnection();
        var country = await conn.QuerySingleOrDefaultAsync<CountryResponse>(
            @"SELECT Id, Name, Code2, Code3, NumericCode, PhoneCode, SortOrder
              FROM Countries
              WHERE Id = @Id AND IsActive = 1", new { Id = id });

        if (country == null)
            return NotFound(new { message = "Country not found." });

        return Ok(country);
    }

    #endregion

    #region Provinces/States

    /// <summary>
    /// Get provinces/states for a country
    /// </summary>
    [HttpGet("countries/{countryId:int}/provinces")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<List<ProvinceStateResponse>>> GetProvinces(int countryId)
    {
        using var conn = CreateConnection();
        var provinces = await conn.QueryAsync<ProvinceStateResponse>(
            @"SELECT Id, CountryId, Name, Code, Type, SortOrder
              FROM ProvinceStates
              WHERE CountryId = @CountryId AND IsActive = 1
              ORDER BY SortOrder, Name", new { CountryId = countryId });

        return Ok(provinces.ToList());
    }

    /// <summary>
    /// Get a single province/state by ID
    /// </summary>
    [HttpGet("provinces/{id:int}")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<ProvinceStateDetailResponse>> GetProvince(int id)
    {
        using var conn = CreateConnection();
        var province = await conn.QuerySingleOrDefaultAsync<ProvinceStateDetailResponse>(
            @"SELECT p.Id, p.CountryId, p.Name, p.Code, p.Type, p.SortOrder,
                     c.Name AS CountryName, c.Code2 AS CountryCode
              FROM ProvinceStates p
              JOIN Countries c ON p.CountryId = c.Id
              WHERE p.Id = @Id AND p.IsActive = 1", new { Id = id });

        if (province == null)
            return NotFound(new { message = "Province/state not found." });

        return Ok(province);
    }

    #endregion

    #region Cities

    /// <summary>
    /// Get cities for a province/state
    /// </summary>
    [HttpGet("provinces/{provinceId:int}/cities")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<List<CityResponse>>> GetCities(int provinceId)
    {
        using var conn = CreateConnection();
        var cities = await conn.QueryAsync<CityResponse>(
            @"SELECT Id, ProvinceStateId, Name, Latitude, Longitude
              FROM Cities
              WHERE ProvinceStateId = @ProvinceStateId AND IsActive = 1
              ORDER BY Name", new { ProvinceStateId = provinceId });

        return Ok(cities.ToList());
    }

    /// <summary>
    /// Search cities by name (across all provinces)
    /// </summary>
    [HttpGet("cities/search")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<List<CitySearchResponse>>> SearchCities(
        [FromQuery] string query,
        [FromQuery] int? countryId = null,
        [FromQuery] int? provinceId = null,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest(new { message = "Search query must be at least 2 characters." });

        limit = Math.Min(limit, 100);

        using var conn = CreateConnection();
        var cities = await conn.QueryAsync<CitySearchResponse>(
            @"SELECT TOP (@Limit)
                     c.Id, c.Name, c.Latitude, c.Longitude,
                     p.Id AS ProvinceStateId, p.Name AS ProvinceStateName, p.Code AS ProvinceStateCode,
                     co.Id AS CountryId, co.Name AS CountryName, co.Code2 AS CountryCode
              FROM Cities c
              JOIN ProvinceStates p ON c.ProvinceStateId = p.Id
              JOIN Countries co ON p.CountryId = co.Id
              WHERE c.IsActive = 1
                AND c.Name LIKE @Query
                AND (@CountryId IS NULL OR co.Id = @CountryId)
                AND (@ProvinceId IS NULL OR p.Id = @ProvinceId)
              ORDER BY c.Name",
            new { Query = $"{query}%", CountryId = countryId, ProvinceId = provinceId, Limit = limit });

        return Ok(cities.ToList());
    }

    /// <summary>
    /// Get a single city by ID
    /// </summary>
    [HttpGet("cities/{id:int}")]
    [ApiKeyAuthorize(ApiScopes.GeoRead, AllowJwt = true)]
    public async Task<ActionResult<CityDetailResponse>> GetCity(int id)
    {
        using var conn = CreateConnection();
        var city = await conn.QuerySingleOrDefaultAsync<CityDetailResponse>(
            @"SELECT c.Id, c.Name, c.Latitude, c.Longitude,
                     p.Id AS ProvinceStateId, p.Name AS ProvinceStateName, p.Code AS ProvinceStateCode,
                     co.Id AS CountryId, co.Name AS CountryName, co.Code2 AS CountryCode
              FROM Cities c
              JOIN ProvinceStates p ON c.ProvinceStateId = p.Id
              JOIN Countries co ON p.CountryId = co.Id
              WHERE c.Id = @Id AND c.IsActive = 1", new { Id = id });

        if (city == null)
            return NotFound(new { message = "City not found." });

        return Ok(city);
    }

    /// <summary>
    /// Create a new city (for user-added cities)
    /// </summary>
    [HttpPost("cities")]
    [ApiKeyAuthorize(ApiScopes.GeoWrite, AllowJwt = true)]
    public async Task<ActionResult<CityResponse>> CreateCity([FromBody] CreateCityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "City name is required." });

        var userId = GetCurrentUserId();

        using var conn = CreateConnection();

        // Verify province exists
        var provinceExists = await conn.ExecuteScalarAsync<bool>(
            "SELECT CASE WHEN EXISTS(SELECT 1 FROM ProvinceStates WHERE Id = @Id AND IsActive = 1) THEN 1 ELSE 0 END",
            new { Id = request.ProvinceStateId });

        if (!provinceExists)
            return BadRequest(new { message = "Province/state not found." });

        // Check for duplicate
        var existingCity = await conn.QuerySingleOrDefaultAsync<int?>(
            @"SELECT Id FROM Cities
              WHERE ProvinceStateId = @ProvinceStateId AND Name = @Name AND IsActive = 1",
            new { request.ProvinceStateId, request.Name });

        if (existingCity.HasValue)
            return Conflict(new { message = "City already exists.", cityId = existingCity.Value });

        // Create city
        var newId = await conn.QuerySingleAsync<int>(
            @"INSERT INTO Cities (ProvinceStateId, Name, Latitude, Longitude, CreatedByUserId)
              OUTPUT INSERTED.Id
              VALUES (@ProvinceStateId, @Name, @Latitude, @Longitude, @CreatedByUserId)",
            new
            {
                request.ProvinceStateId,
                request.Name,
                request.Latitude,
                request.Longitude,
                CreatedByUserId = userId
            });

        _logger.LogInformation("City {CityId} '{Name}' created by user {UserId}",
            newId, request.Name, userId);

        return Ok(new CityResponse
        {
            Id = newId,
            ProvinceStateId = request.ProvinceStateId,
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        });
    }

    /// <summary>
    /// Update city GPS coordinates
    /// </summary>
    [HttpPut("cities/{id:int}/gps")]
    [ApiKeyAuthorize(ApiScopes.GeoWrite, AllowJwt = true)]
    public async Task<ActionResult<CityResponse>> UpdateCityGps(int id, [FromBody] UpdateGpsRequest request)
    {
        using var conn = CreateConnection();

        var rowsAffected = await conn.ExecuteAsync(
            @"UPDATE Cities SET Latitude = @Latitude, Longitude = @Longitude
              WHERE Id = @Id AND IsActive = 1",
            new { Id = id, request.Latitude, request.Longitude });

        if (rowsAffected == 0)
            return NotFound(new { message = "City not found." });

        var city = await conn.QuerySingleAsync<CityResponse>(
            @"SELECT Id, ProvinceStateId, Name, Latitude, Longitude
              FROM Cities WHERE Id = @Id", new { Id = id });

        _logger.LogInformation("City {CityId} GPS updated to ({Lat}, {Lng})",
            id, request.Latitude, request.Longitude);

        return Ok(city);
    }

    #endregion

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

#region DTOs

public class CountryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code2 { get; set; } = string.Empty;
    public string Code3 { get; set; } = string.Empty;
    public string? NumericCode { get; set; }
    public string? PhoneCode { get; set; }
    public int SortOrder { get; set; }
}

public class ProvinceStateResponse
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int SortOrder { get; set; }
}

public class ProvinceStateDetailResponse : ProvinceStateResponse
{
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class CityResponse
{
    public int Id { get; set; }
    public int ProvinceStateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class CitySearchResponse : CityResponse
{
    public string ProvinceStateName { get; set; } = string.Empty;
    public string ProvinceStateCode { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class CityDetailResponse : CitySearchResponse { }

public class CreateCityRequest
{
    public int ProvinceStateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class UpdateGpsRequest
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

#endregion
