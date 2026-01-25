using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

[Table("Cities")]
public class City
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProvinceStateId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// City center latitude for LBS queries
    /// </summary>
    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// City center longitude for LBS queries
    /// </summary>
    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    // Navigation
    [ForeignKey(nameof(ProvinceStateId))]
    public virtual ProvinceState? ProvinceState { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
}
