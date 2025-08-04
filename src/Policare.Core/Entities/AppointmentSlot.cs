namespace PoliCare.Core.Entities;

// Rappresenta uno slot disponibile per appuntamenti (generato dall'algoritmo)
public class AppointmentSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid RoomId { get; set; }
    public bool IsAvailable { get; set; }
    public double UtilizationScore { get; set; } // 0-1, quanto è ottimale questo slot
    public List<string> ConflictReasons { get; set; } = new List<string>();
}