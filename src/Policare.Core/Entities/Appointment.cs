using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid ProfessionalId { get; set; }
    public virtual Professional Professional { get; set; } = null!;

    public Guid RoomId { get; set; }
    public virtual Room Room { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Scheduled, Confirmed, InProgress, Completed, Cancelled, NoShow

    [MaxLength(100)]
    public string? ServiceType { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public decimal? Price { get; set; }
    public bool IsPaid { get; set; }

    // For recurring appointments
    public Guid? RecurringGroupId { get; set; }

    // For follow-up appointments
    public Guid? ParentAppointmentId { get; set; }
    public virtual Appointment? ParentAppointment { get; set; }

    // Navigation properties
    public virtual ICollection<Appointment> FollowUpAppointments { get; set; } = new List<Appointment>();
}