using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storageService;
    private readonly ILogger<SettingsController> _logger;

    private const string MainLogoCategory = "system";
    private const string MainLogoSiteKey = "main-logo";

    public SettingsController(
        ApplicationDbContext context,
        IFileStorageService storageService,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Get the main site logo (public endpoint)
    /// </summary>
    [HttpGet("logo")]
    [AllowAnonymous]
    public async Task<ActionResult<MainLogoResponse>> GetMainLogo()
    {
        var logo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (logo == null)
        {
            return Ok(new MainLogoResponse { HasLogo = false });
        }

        return Ok(new MainLogoResponse
        {
            HasLogo = true,
            LogoUrl = $"/asset/{logo.Id}",
            FileName = logo.FileName
        });
    }

    /// <summary>
    /// Upload a new main site logo (admin only)
    /// </summary>
    [HttpPost("logo")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<MainLogoResponse>> UploadMainLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, GIF, WebP, SVG" });
        }

        // Limit file size to 2MB
        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size must be less than 2MB." });
        }

        // Delete old logo if exists
        var oldLogo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .FirstOrDefaultAsync();

        if (oldLogo != null)
        {
            try
            {
                await _storageService.DeleteFileAsync(oldLogo.StorageUrl);
                _context.Assets.Remove(oldLogo);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old main logo");
            }
        }

        // Create new asset record
        var asset = new Asset
        {
            AssetType = AssetTypes.Image,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StorageUrl = string.Empty,
            StorageType = _storageService.StorageType,
            Category = MainLogoCategory,
            SiteKey = MainLogoSiteKey,
            IsPublic = true
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Upload with asset ID
        var storageUrl = await _storageService.UploadFileAsync(file, asset.Id, MainLogoSiteKey);
        asset.StorageUrl = storageUrl;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Main logo uploaded: Asset {AssetId}", asset.Id);

        return Ok(new MainLogoResponse
        {
            HasLogo = true,
            LogoUrl = $"/asset/{asset.Id}",
            FileName = asset.FileName
        });
    }

    /// <summary>
    /// Delete the main site logo (admin only)
    /// </summary>
    [HttpDelete("logo")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult> DeleteMainLogo()
    {
        var logo = await _context.Assets
            .Where(a => a.Category == MainLogoCategory && a.SiteKey == MainLogoSiteKey)
            .FirstOrDefaultAsync();

        if (logo == null)
        {
            return NotFound(new { message = "No main logo found." });
        }

        try
        {
            await _storageService.DeleteFileAsync(logo.StorageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete main logo file");
        }

        _context.Assets.Remove(logo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Main logo deleted");

        return Ok(new { message = "Main logo deleted successfully." });
    }

    /// <summary>
    /// Get Terms of Service content (public endpoint)
    /// </summary>
    [HttpGet("terms-of-service")]
    [AllowAnonymous]
    public async Task<ActionResult<LegalContentResponse>> GetTermsOfService()
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.TermsOfService);

        return Ok(new LegalContentResponse
        {
            Content = setting?.Value ?? "",
            UpdatedAt = setting?.UpdatedAt
        });
    }

    /// <summary>
    /// Update Terms of Service content (admin only)
    /// </summary>
    [HttpPut("terms-of-service")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<LegalContentResponse>> UpdateTermsOfService([FromBody] LegalContentRequest request)
    {
        var userId = GetCurrentUserId();
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.TermsOfService);

        if (setting == null)
        {
            setting = new Setting
            {
                Key = SettingKeys.TermsOfService,
                Value = request.Content ?? "",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = request.Content ?? "";
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = userId;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Terms of Service updated by user {UserId}", userId);

        return Ok(new LegalContentResponse
        {
            Content = setting.Value,
            UpdatedAt = setting.UpdatedAt
        });
    }

    /// <summary>
    /// Get Privacy Policy content (public endpoint)
    /// </summary>
    [HttpGet("privacy-policy")]
    [AllowAnonymous]
    public async Task<ActionResult<LegalContentResponse>> GetPrivacyPolicy()
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.PrivacyPolicy);

        return Ok(new LegalContentResponse
        {
            Content = setting?.Value ?? "",
            UpdatedAt = setting?.UpdatedAt
        });
    }

    /// <summary>
    /// Update Privacy Policy content (admin only)
    /// </summary>
    [HttpPut("privacy-policy")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<LegalContentResponse>> UpdatePrivacyPolicy([FromBody] LegalContentRequest request)
    {
        var userId = GetCurrentUserId();
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == SettingKeys.PrivacyPolicy);

        if (setting == null)
        {
            setting = new Setting
            {
                Key = SettingKeys.PrivacyPolicy,
                Value = request.Content ?? "",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = request.Content ?? "";
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = userId;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Privacy Policy updated by user {UserId}", userId);

        return Ok(new LegalContentResponse
        {
            Content = setting.Value,
            UpdatedAt = setting.UpdatedAt
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class MainLogoResponse
{
    public bool HasLogo { get; set; }
    public string? LogoUrl { get; set; }
    public string? FileName { get; set; }
}

public class LegalContentResponse
{
    public string Content { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
}

public class LegalContentRequest
{
    public string? Content { get; set; }
}
