using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

public class Professional : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? LicenseNumber { get; set; }

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public Guid ClinicId { get; set; }
    public virtual Clinic Clinic { get; set; } = null!;

    public int DefaultAppointmentDuration { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<ProfessionalAvailability> Availabilities { get; set; } = new List<ProfessionalAvailability>();
}