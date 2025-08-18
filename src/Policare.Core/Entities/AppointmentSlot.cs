public class AppointmentSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RoomId { get; set; }
    public bool IsAvailable { get; set; }

    // SMART SCORING
    public double OptimalityScore { get; set; } // 0-100, quanto è ottimale questo slot
    public double UtilizationScore { get; set; } // 0-100, efficienza utilizzo risorse
    public double PreferenceScore { get; set; } // 0-100, quanto rispetta le preferenze

    // CONFLICT ANALYSIS
    public List<string> ConflictReasons { get; set; } = new List<string>();
    public List<string> OptimizationFactors { get; set; } = new List<string>();

    // METADATA
    public string DoctorName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
}