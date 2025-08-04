namespace PoliCare.Core.Entities;

public class TimeSlot : BaseEntity
{
    public Guid DoctorId { get; set; }

    public Guid RoomId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int RecurrenceWeeks { get; set; } = 1; // Ogni quante settimane si ripete

    // Navigation Properties
    public virtual Doctor Doctor { get; set; } = null!;
    public virtual Room Room { get; set; } = null!;
}