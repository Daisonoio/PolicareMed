namespace PoliCare.Core.Entities;

public class RoomAvailability : BaseEntity
{
    public Guid RoomId { get; set; }
    public virtual Room Room { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // For specific date ranges
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;
}