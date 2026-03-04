using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationPipelineService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        ApplicationDbContext context,
        INotificationPipelineService notificationService,
        ILogger<NotificationController> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ========================================================================
    // Sending Notifications (for sites)
    // ========================================================================

    /// <summary>
    /// Send a notification - called by partner sites
    /// Requires API key with notifications:send scope
    /// </summary>
    [HttpPost("send")]
    [ApiKeyAuthorize(ApiScopes.NotificationsSend)]
    public async Task<ActionResult<SendNotificationResponse>> Send([FromBody] SendNotificationRequest request)
    {
        // Get siteKey from API key context
        var siteKey = HttpContext.Items["SiteKey"]?.ToString();
        if (string.IsNullOrEmpty(siteKey))
        {
            return BadRequest(new SendNotificationResponse 
            { 
                Success = false, 
                Message = "API key must be associated with a site" 
            });
        }

        if (string.IsNullOrEmpty(request.Type))
        {
            return BadRequest(new SendNotificationResponse 
            { 
                Success = false, 
                Message = "Notification type is required" 
            });
        }

        if (request.UserId == null && request.Recipient == null)
        {
            return BadRequest(new SendNotificationResponse 
            { 
                Success = false, 
                Message = "Either userId or recipient info is required" 
            });
        }

        var response = await _notificationService.SendAsync(siteKey, request);
        return Ok(response);
    }

    // ========================================================================
    // Queue Management (for admins)
    // ========================================================================

    /// <summary>
    /// Get notification queue with filters
    /// </summary>
    [HttpGet("queue")]
    [Authorize]
    public async Task<ActionResult<NotificationQueueResponse>> GetQueue(
        [FromQuery] string? siteKey,
        [FromQuery] string? status,
        [FromQuery] string? typeCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Check if user is SU or site admin
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        // Non-SU users can only see their site's notifications
        if (user.SystemRole != "SU" && !string.IsNullOrEmpty(siteKey))
        {
            // TODO: Check if user is admin for this site
        }

        var response = await _notificationService.GetQueueAsync(siteKey, status, typeCode, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific notification
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<NotificationQueueItem>> GetById(long id)
    {
        var item = await _notificationService.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Approve a held notification
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize]
    public async Task<ActionResult> Approve(long id, [FromBody] ModerateRequest? request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var success = await _notificationService.ApproveAsync(id, user.Id, request?.Note);
        if (!success) return NotFound("Notification not found or not in Held status");

        return Ok(new { success = true, message = "Notification approved and sent" });
    }

    /// <summary>
    /// Reject a held notification
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize]
    public async Task<ActionResult> Reject(long id, [FromBody] ModerateRequest? request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var success = await _notificationService.RejectAsync(id, user.Id, request?.Note);
        if (!success) return NotFound("Notification not found or not in Held status");

        return Ok(new { success = true, message = "Notification rejected" });
    }

    /// <summary>
    /// Retry a failed notification
    /// </summary>
    [HttpPost("{id}/retry")]
    [Authorize]
    public async Task<ActionResult> Retry(long id)
    {
        var success = await _notificationService.RetryAsync(id);
        if (!success) return NotFound("Notification not found or not in Failed status");

        return Ok(new { success = true, message = "Notification queued for retry" });
    }

    // ========================================================================
    // Template Management
    // ========================================================================

    /// <summary>
    /// List templates for a site
    /// </summary>
    [HttpGet("templates")]
    [Authorize]
    public async Task<ActionResult<List<NotificationTemplate>>> GetTemplates(
        [FromQuery] string? siteKey,
        [FromQuery] string? typeCode,
        [FromQuery] string? channelCode)
    {
        var query = _context.Set<NotificationTemplate>().AsQueryable();

        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(t => t.SiteKey == siteKey);
        if (!string.IsNullOrEmpty(typeCode))
            query = query.Where(t => t.TypeCode == typeCode);
        if (!string.IsNullOrEmpty(channelCode))
            query = query.Where(t => t.ChannelCode == channelCode);

        var templates = await query
            .OrderBy(t => t.SiteKey)
            .ThenBy(t => t.TypeCode)
            .ThenBy(t => t.ChannelCode)
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Get a specific template
    /// </summary>
    [HttpGet("templates/{id}")]
    [Authorize]
    public async Task<ActionResult<NotificationTemplate>> GetTemplate(int id)
    {
        var template = await _context.Set<NotificationTemplate>().FindAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    /// <summary>
    /// Create a new template
    /// </summary>
    [HttpPost("templates")]
    [Authorize]
    public async Task<ActionResult<NotificationTemplate>> CreateTemplate([FromBody] TemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        // Check if template already exists
        var existing = await _context.Set<NotificationTemplate>()
            .AnyAsync(t => t.SiteKey == request.SiteKey 
                && t.TypeCode == request.TypeCode 
                && t.ChannelCode == request.ChannelCode
                && t.LangCode == request.LangCode);

        if (existing)
        {
            return BadRequest(new { message = "Template already exists for this combination" });
        }

        var template = new NotificationTemplate
        {
            SiteKey = request.SiteKey,
            TypeCode = request.TypeCode,
            ChannelCode = request.ChannelCode,
            LangCode = request.LangCode,
            Subject = request.Subject,
            BodyHtml = request.BodyHtml,
            BodyText = request.BodyText,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

        _context.Set<NotificationTemplate>().Add(template);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    /// <summary>
    /// Update a template
    /// </summary>
    [HttpPut("templates/{id}")]
    [Authorize]
    public async Task<ActionResult<NotificationTemplate>> UpdateTemplate(int id, [FromBody] TemplateRequest request)
    {
        var template = await _context.Set<NotificationTemplate>().FindAsync(id);
        if (template == null) return NotFound();

        template.Subject = request.Subject;
        template.BodyHtml = request.BodyHtml;
        template.BodyText = request.BodyText;
        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(template);
    }

    /// <summary>
    /// Preview a template with sample data
    /// </summary>
    [HttpPost("templates/{id}/preview")]
    [Authorize]
    public async Task<ActionResult<TemplatePreviewResponse>> PreviewTemplate(
        int id, 
        [FromBody] TemplatePreviewRequest request)
    {
        var template = await _context.Set<NotificationTemplate>().FindAsync(id);
        if (template == null) return NotFound();

        var data = request.Data ?? new Dictionary<string, object>();
        
        // Simple rendering
        string? Render(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var kvp in data)
            {
                text = text.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
            }
            return text;
        }

        return Ok(new TemplatePreviewResponse
        {
            Subject = Render(template.Subject),
            BodyHtml = Render(template.BodyHtml),
            BodyText = Render(template.BodyText)
        });
    }

    // ========================================================================
    // Notification Types
    // ========================================================================

    /// <summary>
    /// List notification types
    /// </summary>
    [HttpGet("types")]
    [Authorize]
    public async Task<ActionResult<List<NotificationType>>> GetTypes()
    {
        var types = await _context.Set<NotificationType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return Ok(types);
    }

    // ========================================================================
    // Moderation Rules
    // ========================================================================

    /// <summary>
    /// List moderation rules
    /// </summary>
    [HttpGet("rules")]
    [Authorize]
    public async Task<ActionResult<List<ModerationRule>>> GetRules([FromQuery] string? siteKey)
    {
        var query = _context.Set<ModerationRule>().AsQueryable();

        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(r => r.SiteKey == null || r.SiteKey == siteKey);

        var rules = await query
            .OrderBy(r => r.Priority)
            .ToListAsync();

        return Ok(rules);
    }

    // ========================================================================
    // Webhook (for FXNotification callbacks)
    // ========================================================================

    /// <summary>
    /// Webhook endpoint for FXNotification delivery status updates
    /// </summary>
    [HttpPost("{id}/webhook")]
    public async Task<ActionResult> Webhook(long id, [FromBody] FxWebhookPayload payload)
    {
        var item = await _context.Set<NotificationQueueItem>().FindAsync(id);
        if (item == null) return NotFound();

        if (payload.Status == "Sent" || payload.Status == "Delivered")
        {
            item.Status = "Sent";
            item.SentAt = DateTime.UtcNow;
        }
        else if (payload.Status == "Failed")
        {
            item.Status = "Failed";
            item.ErrorMessage = payload.Error;
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return null;
        return await _context.Users.FindAsync(userId);
    }
}

public class FxWebhookPayload
{
    public string? Status { get; set; }
    public string? Error { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
