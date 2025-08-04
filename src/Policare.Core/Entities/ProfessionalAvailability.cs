namespace PoliCare.Core.Entities;

public class ProfessionalAvailability : BaseEntity
{
    public Guid ProfessionalId { get; set; }
    public virtual Professional Professional { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // For specific date ranges
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;
}