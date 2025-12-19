using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

public class OtpRateLimit
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public int RequestCount { get; set; } = 0;

    public DateTime WindowStart { get; set; } = DateTime.UtcNow;

    public DateTime? BlockedUntil { get; set; }
}
