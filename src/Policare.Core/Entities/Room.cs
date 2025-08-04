using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

public class Room : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Foreign Key
    public Guid ClinicId { get; set; }

    // Navigation Properties
    public virtual Clinic Clinic { get; set; } = null!;
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<RoomAvailability> Availabilities { get; set; } = new List<RoomAvailability>();
}