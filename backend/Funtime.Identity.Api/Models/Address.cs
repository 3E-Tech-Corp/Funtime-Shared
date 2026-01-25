using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

[Table("Addresses")]
public class Address
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Links to Cities table (which links to ProvinceState -> Country)
    /// </summary>
    [Required]
    public int CityId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Line2 { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Precise latitude for this address (building/house level)
    /// </summary>
    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Precise longitude for this address (building/house level)
    /// </summary>
    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Whether GPS coordinates have been verified (e.g., via map pin)
    /// </summary>
    public bool IsVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User who created this address (for audit purposes, not ownership)
    /// </summary>
    public int? CreatedByUserId { get; set; }

    // Navigation
    [ForeignKey(nameof(CityId))]
    public virtual City? City { get; set; }
}
