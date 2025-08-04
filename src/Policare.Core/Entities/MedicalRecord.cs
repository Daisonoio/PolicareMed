using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public MedicalRecordType Type { get; set; }

    public string Attachments { get; set; } = "[]"; // JSON array con file paths

    public bool IsSharedWithPatient { get; set; } = false;

    // Navigation Properties
    public virtual Patient Patient { get; set; } = null!;
    public virtual Doctor Doctor { get; set; } = null!;
    public virtual Appointment? Appointment { get; set; }
}