namespace Funtime.Identity.Api.Models;

/// <summary>
/// SMTP mail profile matching FXNotification.MailProfiles table
/// </summary>
public class MailProfileRow
{
    public int ProfileId { get; set; }
    public string? ProfileCode { get; set; }
    public int? App_ID { get; set; }
    public string? FromName { get; set; }
    public string? FromEmail { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? AuthUser { get; set; }
    public string? AuthSecretRef { get; set; }
    public string? SecurityMode { get; set; } = "StartTlsWhenAvailable";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Application registration matching FXNotification.Apps table
/// </summary>
public class AppRow
{
    public int App_ID { get; set; }
    public string? App_Code { get; set; }
    public string? Descr { get; set; }
    public int? ProfileID { get; set; }
    public string? ApiKey { get; set; }
    public string? AllowedTasks { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int RequestCount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for apps — masks the API key
/// </summary>
public class AppResponseDto
{
    public int App_ID { get; set; }
    public string? App_Code { get; set; }
    public string? Descr { get; set; }
    public int? ProfileID { get; set; }
    public string? MaskedKey { get; set; }
    public string? FullKey { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int RequestCount { get; set; }
    public string? Notes { get; set; }

    public static AppResponseDto FromRow(AppRow row, string? fullKey = null) => new()
    {
        App_ID = row.App_ID,
        App_Code = row.App_Code,
        Descr = row.Descr,
        ProfileID = row.ProfileID,
        MaskedKey = MaskKey(row.ApiKey),
        FullKey = fullKey,
        IsActive = row.IsActive,
        CreatedAt = row.CreatedAt,
        LastUsedAt = row.LastUsedAt,
        RequestCount = row.RequestCount,
        Notes = row.Notes,
    };

    private static string? MaskKey(string? key)
        => key is { Length: > 12 } ? key[..8] + "****" + key[^4..] : key != null ? "****" : null;
}
