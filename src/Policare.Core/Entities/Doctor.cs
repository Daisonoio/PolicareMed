using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

public class Doctor : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid ClinicId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }

    public decimal CommissionPercentage { get; set; } = 0.7m; // 70% default

    public string WorkingHours { get; set; } = "{}"; // JSON con orari di lavoro

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Clinic Clinic { get; set; } = null!;
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
}