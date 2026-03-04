using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Services;

public interface INotificationPipelineService
{
    Task<SendNotificationResponse> SendAsync(string siteKey, SendNotificationRequest request);
    Task<NotificationQueueItem?> GetByIdAsync(long id);
    Task<NotificationQueueResponse> GetQueueAsync(string? siteKey, string? status, string? typeCode, int page, int pageSize);
    Task<bool> ApproveAsync(long id, int moderatorId, string? note);
    Task<bool> RejectAsync(long id, int moderatorId, string? note);
    Task<bool> RetryAsync(long id);
    Task ProcessPendingAsync(); // Background worker calls this
}

public class NotificationPipelineService : INotificationPipelineService
{
    private readonly ApplicationDbContext _context;
    private readonly IFxNotificationClient _fxClient;
    private readonly ILogger<NotificationPipelineService> _logger;

    public NotificationPipelineService(
        ApplicationDbContext context,
        IFxNotificationClient fxClient,
        ILogger<NotificationPipelineService> logger)
    {
        _context = context;
        _fxClient = fxClient;
        _logger = logger;
    }

    public async Task<SendNotificationResponse> SendAsync(string siteKey, SendNotificationRequest request)
    {
        var response = new SendNotificationResponse { Success = true };
        
        // Get recipient info
        string? email = request.Recipient?.Email;
        string? phone = request.Recipient?.Phone;
        string? name = request.Recipient?.Name;

        if (request.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(request.UserId.Value);
            if (user != null)
            {
                email ??= user.Email;
                phone ??= user.PhoneNumber;
                name ??= $"{user.FirstName} {user.LastName}".Trim();
            }
        }

        // Get channels to send on
        var channels = request.Channels ?? new List<string> { "email" }; // Default to email
        
        // Get configured channels for this site
        var channelConfigs = await _context.Set<ChannelConfig>()
            .Where(c => c.SiteKey == siteKey && c.IsEnabled && channels.Contains(c.ChannelCode))
            .ToListAsync();

        if (!channelConfigs.Any())
        {
            return new SendNotificationResponse
            {
                Success = false,
                Message = "No enabled channels configured for this site"
            };
        }

        var templateData = request.Data != null 
            ? JsonSerializer.Serialize(request.Data) 
            : null;

        // Check moderation rules once
        var moderationAction = await GetModerationActionAsync(siteKey, request.Type, null);

        foreach (var channelConfig in channelConfigs)
        {
            // Get template
            var template = await _context.Set<NotificationTemplate>()
                .Where(t => t.SiteKey == siteKey 
                    && t.TypeCode == request.Type 
                    && t.ChannelCode == channelConfig.ChannelCode
                    && t.IsActive)
                .OrderByDescending(t => t.LangCode == "en") // Prefer English for now
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogWarning("No template found for {SiteKey}/{Type}/{Channel}", 
                    siteKey, request.Type, channelConfig.ChannelCode);
                continue;
            }

            // Render template
            var rendered = RenderTemplate(template, request.Data ?? new Dictionary<string, object>());

            // Determine status based on moderation
            var status = moderationAction switch
            {
                "Hold" => "Held",
                "Block" => "Cancelled",
                _ => "Pending"
            };

            // Create queue item
            var queueItem = new NotificationQueueItem
            {
                SiteKey = siteKey,
                TypeCode = request.Type,
                UserId = request.UserId,
                RecipientEmail = email,
                RecipientPhone = phone,
                RecipientName = name,
                ChannelCode = channelConfig.ChannelCode,
                Subject = rendered.Subject,
                BodyHtml = rendered.BodyHtml,
                BodyText = rendered.BodyText,
                TemplateData = templateData,
                Status = status,
                ModerationStatus = status == "Held" ? "Held" : null,
                Priority = request.Priority ?? 5,
                ScheduledFor = request.ScheduledFor,
                ExpiresAt = request.ExpiresAt,
                ObjectId = request.ObjectId,
                ObjectType = request.ObjectType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<NotificationQueueItem>().Add(queueItem);
            await _context.SaveChangesAsync();

            response.NotificationIds.Add(queueItem.Id);

            // Log creation
            await LogActionAsync(queueItem.Id, "Created", new { siteKey, request.Type, channelConfig.ChannelCode });

            // If auto-send and not scheduled for later, dispatch immediately
            if (status == "Pending" && !request.ScheduledFor.HasValue)
            {
                await DispatchAsync(queueItem, channelConfig);
            }
        }

        response.Status = moderationAction == "Hold" ? "Held" : "Pending";
        response.Message = moderationAction == "Hold" 
            ? "Notifications queued for moderation review"
            : $"Queued {response.NotificationIds.Count} notification(s)";

        return response;
    }

    public async Task<NotificationQueueItem?> GetByIdAsync(long id)
    {
        return await _context.Set<NotificationQueueItem>().FindAsync(id);
    }

    public async Task<NotificationQueueResponse> GetQueueAsync(
        string? siteKey, string? status, string? typeCode, int page, int pageSize)
    {
        var query = _context.Set<NotificationQueueItem>().AsQueryable();

        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(q => q.SiteKey == siteKey);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);
        if (!string.IsNullOrEmpty(typeCode))
            query = query.Where(q => q.TypeCode == typeCode);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new NotificationQueueResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ApproveAsync(long id, int moderatorId, string? note)
    {
        var item = await _context.Set<NotificationQueueItem>().FindAsync(id);
        if (item == null || item.Status != "Held") return false;

        item.Status = "Pending";
        item.ModerationStatus = "Approved";
        item.ModerationNote = note;
        item.ModeratedBy = moderatorId;
        item.ModeratedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogActionAsync(id, "Approved", new { moderatorId, note });

        // Get channel config and dispatch
        var channelConfig = await _context.Set<ChannelConfig>()
            .FirstOrDefaultAsync(c => c.SiteKey == item.SiteKey && c.ChannelCode == item.ChannelCode);
        
        if (channelConfig != null)
        {
            await DispatchAsync(item, channelConfig);
        }

        return true;
    }

    public async Task<bool> RejectAsync(long id, int moderatorId, string? note)
    {
        var item = await _context.Set<NotificationQueueItem>().FindAsync(id);
        if (item == null || item.Status != "Held") return false;

        item.Status = "Cancelled";
        item.ModerationStatus = "Rejected";
        item.ModerationNote = note;
        item.ModeratedBy = moderatorId;
        item.ModeratedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogActionAsync(id, "Rejected", new { moderatorId, note });

        return true;
    }

    public async Task<bool> RetryAsync(long id)
    {
        var item = await _context.Set<NotificationQueueItem>().FindAsync(id);
        if (item == null || item.Status != "Failed") return false;

        item.Status = "Pending";
        item.ErrorMessage = null;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogActionAsync(id, "Retried", null);

        // Get channel config and dispatch
        var channelConfig = await _context.Set<ChannelConfig>()
            .FirstOrDefaultAsync(c => c.SiteKey == item.SiteKey && c.ChannelCode == item.ChannelCode);
        
        if (channelConfig != null)
        {
            await DispatchAsync(item, channelConfig);
        }

        return true;
    }

    public async Task ProcessPendingAsync()
    {
        // Get pending items that are due (scheduled time passed or no schedule)
        var pendingItems = await _context.Set<NotificationQueueItem>()
            .Where(q => q.Status == "Pending" 
                && (q.ScheduledFor == null || q.ScheduledFor <= DateTime.UtcNow)
                && (q.ExpiresAt == null || q.ExpiresAt > DateTime.UtcNow))
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .Take(50) // Process in batches
            .ToListAsync();

        foreach (var item in pendingItems)
        {
            var channelConfig = await _context.Set<ChannelConfig>()
                .FirstOrDefaultAsync(c => c.SiteKey == item.SiteKey && c.ChannelCode == item.ChannelCode);
            
            if (channelConfig != null)
            {
                await DispatchAsync(item, channelConfig);
            }
        }
    }

    // ========================================================================
    // Private Helpers
    // ========================================================================

    private async Task<string> GetModerationActionAsync(string siteKey, string typeCode, string? channelCode)
    {
        // Get matching rules ordered by priority (lower = higher priority)
        var rules = await _context.Set<ModerationRule>()
            .Where(r => r.IsActive)
            .Where(r => r.SiteKey == null || r.SiteKey == siteKey)
            .Where(r => r.TypeCode == null || r.TypeCode == typeCode)
            .Where(r => r.ChannelCode == null || r.ChannelCode == channelCode)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        // First matching rule wins
        var matchingRule = rules.FirstOrDefault();
        return matchingRule?.Action ?? "AutoSend";
    }

    private async Task DispatchAsync(NotificationQueueItem item, ChannelConfig config)
    {
        try
        {
            item.Status = "Sending";
            item.Attempts++;
            item.LastAttemptAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var configData = !string.IsNullOrEmpty(config.Config) 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(config.Config) 
                : new Dictionary<string, string>();

            switch (item.ChannelCode.ToLower())
            {
                case "email":
                    await DispatchEmailAsync(item, configData);
                    break;
                case "sms":
                    await DispatchSmsAsync(item, configData);
                    break;
                // Add more channels as needed
                default:
                    throw new NotSupportedException($"Channel {item.ChannelCode} not supported");
            }

            item.Status = "Sent";
            item.SentAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LogActionAsync(item.Id, "Sent", new { channel = item.ChannelCode, externalId = item.ExternalId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch notification {Id}", item.Id);
            item.Status = "Failed";
            item.ErrorMessage = ex.Message;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LogActionAsync(item.Id, "Failed", new { error = ex.Message });
        }
    }

    private async Task DispatchEmailAsync(NotificationQueueItem item, Dictionary<string, string>? config)
    {
        var taskCode = config?.GetValueOrDefault("fxTaskCode") ?? "DEFAULT_EMAIL";
        
        var result = await _fxClient.QueueNotificationAsync(new FxQueueRequest
        {
            TaskCode = taskCode,
            To = item.RecipientEmail!,
            BodyJson = JsonSerializer.Serialize(new
            {
                Subject = item.Subject,
                BodyHtml = item.BodyHtml,
                RecipientName = item.RecipientName
            })
        });

        item.ExternalId = result.NotificationId.ToString();
    }

    private async Task DispatchSmsAsync(NotificationQueueItem item, Dictionary<string, string>? config)
    {
        var taskCode = config?.GetValueOrDefault("fxTaskCode") ?? "DEFAULT_SMS";
        
        var result = await _fxClient.QueueNotificationAsync(new FxQueueRequest
        {
            TaskCode = taskCode,
            To = item.RecipientPhone!,
            BodyJson = JsonSerializer.Serialize(new
            {
                BodyText = item.BodyText
            })
        });

        item.ExternalId = result.NotificationId.ToString();
    }

    private (string? Subject, string? BodyHtml, string? BodyText) RenderTemplate(
        NotificationTemplate template, 
        Dictionary<string, object> data)
    {
        // Simple Handlebars-style rendering: {{variable}}
        string? Render(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            return Regex.Replace(text, @"\{\{(\w+)\}\}", match =>
            {
                var key = match.Groups[1].Value;
                if (data.TryGetValue(key, out var value))
                {
                    return value?.ToString() ?? "";
                }
                return match.Value; // Keep original if not found
            });
        }

        return (Render(template.Subject), Render(template.BodyHtml), Render(template.BodyText));
    }

    private async Task LogActionAsync(long notificationId, string action, object? details)
    {
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO NotificationLog (NotificationId, Action, Details, CreatedAt)
              VALUES ({0}, {1}, {2}, GETUTCDATE())",
            notificationId, action, details != null ? JsonSerializer.Serialize(details) : null);
    }
}

// ============================================================================
// FXNotification Client
// ============================================================================

public interface IFxNotificationClient
{
    Task<FxQueueResponse> QueueNotificationAsync(FxQueueRequest request);
}

public class FxQueueRequest
{
    public string TaskCode { get; set; } = "";
    public string To { get; set; } = "";
    public string? Cc { get; set; }
    public string? BodyJson { get; set; }
}

public class FxQueueResponse
{
    public long NotificationId { get; set; }
    public string? Message { get; set; }
}

public class FxNotificationClient : IFxNotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FxNotificationClient> _logger;

    public FxNotificationClient(HttpClient httpClient, ILogger<FxNotificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FxQueueResponse> QueueNotificationAsync(FxQueueRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/notifications/queue", request);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<FxApiResponse<FxQueueResponse>>();
        return result?.Data ?? new FxQueueResponse();
    }
}

public class FxApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
