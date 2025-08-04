using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

public class Patient : BaseEntity
{
    public Guid ClinicId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(16)]
    public string FiscalCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public string MedicalHistory { get; set; } = "{}"; // JSON con storia medica

    public string Preferences { get; set; } = "{}"; // JSON con preferenze

    // Navigation Properties
    public virtual Clinic Clinic { get; set; } = null!;
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}