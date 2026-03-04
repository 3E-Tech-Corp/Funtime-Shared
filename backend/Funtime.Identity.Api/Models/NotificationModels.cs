using System.Text.Json.Serialization;

namespace Funtime.Identity.Api.Models;

// ============================================================================
// Database Entities
// ============================================================================

public class NotificationType
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class NotificationTemplate
{
    public int Id { get; set; }
    public string SiteKey { get; set; } = "";
    public string TypeCode { get; set; } = "";
    public string ChannelCode { get; set; } = "";
    public string LangCode { get; set; } = "en";
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
}

public class NotificationQueueItem
{
    public long Id { get; set; }
    public string SiteKey { get; set; } = "";
    public string TypeCode { get; set; } = "";
    public int? UserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientName { get; set; }
    public string ChannelCode { get; set; } = "";
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public string? TemplateData { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ModerationStatus { get; set; }
    public string? ModerationNote { get; set; }
    public int? ModeratedBy { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public string? ExternalId { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int Priority { get; set; } = 5;
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long? ObjectId { get; set; }
    public string? ObjectType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ModerationRule
{
    public int Id { get; set; }
    public string? SiteKey { get; set; }
    public string? TypeCode { get; set; }
    public string? ChannelCode { get; set; }
    public string Action { get; set; } = "AutoSend"; // AutoSend, Hold, Block
    public string? Conditions { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ChannelConfig
{
    public int Id { get; set; }
    public string SiteKey { get; set; } = "";
    public string ChannelCode { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string? Config { get; set; } // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// ============================================================================
// API Request/Response DTOs
// ============================================================================

public class SendNotificationRequest
{
    public string Type { get; set; } = "";
    
    // Recipient - either userId OR direct recipient info
    public int? UserId { get; set; }
    public RecipientInfo? Recipient { get; set; }
    
    // Optional: specify channels (defaults to all configured)
    public List<string>? Channels { get; set; }
    
    // Template data
    public Dictionary<string, object>? Data { get; set; }
    
    // Optional scheduling
    public int? Priority { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Optional reference
    public long? ObjectId { get; set; }
    public string? ObjectType { get; set; }
}

public class RecipientInfo
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Name { get; set; }
}

public class SendNotificationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<long> NotificationIds { get; set; } = new();
    public string? Status { get; set; } // Pending, Held
}

public class NotificationQueueResponse
{
    public List<NotificationQueueItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class TemplateRequest
{
    public string SiteKey { get; set; } = "";
    public string TypeCode { get; set; } = "";
    public string ChannelCode { get; set; } = "";
    public string LangCode { get; set; } = "en";
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
}

public class TemplatePreviewRequest
{
    public Dictionary<string, object>? Data { get; set; }
}

public class TemplatePreviewResponse
{
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
}

public class ModerateRequest
{
    public string? Note { get; set; }
}
