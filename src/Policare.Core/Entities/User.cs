using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace PoliCare.Core.Entities;

public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public Guid ClinicId { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation Properties
    public virtual Clinic Clinic { get; set; } = null!;
    public virtual Doctor? Doctor { get; set; }
}