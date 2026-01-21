using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;
using System.Security.Claims;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// API endpoints for sending real-time push notifications via SignalR.
/// External sites can call these endpoints to send notifications to users.
/// Supports API key authentication with push:send scope.
/// </summary>
[ApiController]
[Route("api/push")]
public class PushNotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        INotificationService notificationService,
        ILogger<PushNotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification to a specific user by their user ID (supports API key with push:send scope)
    /// </summary>
    [HttpPost("user/{userId}")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult> SendToUser(int userId, [FromBody] PushNotificationRequest request)
    {
        await _notificationService.SendToUserAsync(userId, request.Type, request.Payload);

        _logger.LogInformation(
            "Push notification sent to user {UserId}, type: {Type}",
            userId, request.Type);

        return Ok(new {
            success = true,
            message = "Notification sent",
            isUserConnected = _notificationService.IsUserConnected(userId)
        });
    }

    /// <summary>
    /// Send a notification to all users on a specific site (supports API key with push:send scope)
    /// </summary>
    [HttpPost("site/{siteKey}")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult> SendToSite(string siteKey, [FromBody] PushNotificationRequest request)
    {
        await _notificationService.SendToSiteAsync(siteKey, request.Type, request.Payload);

        _logger.LogInformation(
            "Push notification sent to site {SiteKey}, type: {Type}",
            siteKey, request.Type);

        return Ok(new { success = true, message = "Notification sent to site" });
    }

    /// <summary>
    /// Send a notification to all connected users (supports API key with push:send scope)
    /// </summary>
    [HttpPost("broadcast")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult> Broadcast([FromBody] PushNotificationRequest request)
    {
        await _notificationService.SendToAllAsync(request.Type, request.Payload);

        _logger.LogInformation(
            "Broadcast notification sent, type: {Type}",
            request.Type);

        return Ok(new { success = true, message = "Broadcast sent" });
    }

    /// <summary>
    /// Check if a user is currently connected to receive notifications (supports API key with push:send scope)
    /// </summary>
    [HttpGet("user/{userId}/status")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public ActionResult<UserConnectionStatus> GetUserStatus(int userId)
    {
        return Ok(new UserConnectionStatus
        {
            UserId = userId,
            IsConnected = _notificationService.IsUserConnected(userId)
        });
    }

    /// <summary>
    /// Send notifications to multiple users at once (supports API key with push:send scope)
    /// </summary>
    [HttpPost("users/batch")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult> SendToUsers([FromBody] BatchNotificationRequest request)
    {
        var results = new List<BatchNotificationResult>();

        foreach (var userId in request.UserIds)
        {
            await _notificationService.SendToUserAsync(userId, request.Type, request.Payload);
            results.Add(new BatchNotificationResult
            {
                UserId = userId,
                IsConnected = _notificationService.IsUserConnected(userId)
            });
        }

        _logger.LogInformation(
            "Batch notification sent to {Count} users, type: {Type}",
            request.UserIds.Count, request.Type);

        return Ok(new {
            success = true,
            message = $"Notification sent to {request.UserIds.Count} users",
            results
        });
    }
}

#region DTOs

public class PushNotificationRequest
{
    /// <summary>
    /// Type of notification (e.g., "message", "alert", "update")
    /// </summary>
    public string Type { get; set; } = "notification";

    /// <summary>
    /// Notification payload - can be any JSON object
    /// </summary>
    public object? Payload { get; set; }
}

public class BatchNotificationRequest
{
    /// <summary>
    /// List of user IDs to send to
    /// </summary>
    public List<int> UserIds { get; set; } = new();

    /// <summary>
    /// Type of notification
    /// </summary>
    public string Type { get; set; } = "notification";

    /// <summary>
    /// Notification payload
    /// </summary>
    public object? Payload { get; set; }
}

public class UserConnectionStatus
{
    public int UserId { get; set; }
    public bool IsConnected { get; set; }
}

public class BatchNotificationResult
{
    public int UserId { get; set; }
    public bool IsConnected { get; set; }
}

#endregion
