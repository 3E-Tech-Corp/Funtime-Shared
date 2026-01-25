using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Funtime.Identity.Api.Models;

[Table("Countries")]
public class Country
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string Code2 { get; set; } = string.Empty;

    [Required]
    [MaxLength(3)]
    public string Code3 { get; set; } = string.Empty;

    [MaxLength(3)]
    public string? NumericCode { get; set; }

    [MaxLength(10)]
    public string? PhoneCode { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<ProvinceState> ProvinceStates { get; set; } = new List<ProvinceState>();
}
