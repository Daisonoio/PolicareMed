using PoliCare.Core.Entities;

namespace Policare.API.DTOs;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ServiceType { get; set; }
    public string? Notes { get; set; }
    public decimal? Price { get; set; }
    public bool IsPaid { get; set; }

    // Nested information for display
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CreateAppointmentDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? ServiceType { get; set; }
    public string? Notes { get; set; }
    public decimal? Price { get; set; }
    public bool IsPaid { get; set; } = false;
}

public class CreateSmartAppointmentDto
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? PreferredDoctorId { get; set; }
    public string? RequiredSpecialization { get; set; }
    public DateTime PreferredDate { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentType Type { get; set; } = AppointmentType.FirstVisit;
    public string? ServiceType { get; set; }
    public string? Notes { get; set; }
    public decimal? Price { get; set; }

    // Smart Scheduling Preferences
    public SmartSchedulingPreferencesDto? Preferences { get; set; }
    public bool AutoOptimize { get; set; } = true;
}

public class SmartSchedulingPreferencesDto
{
    public TimeSpan PreferredStartTime { get; set; } = new TimeSpan(9, 0, 0); // 9:00
    public TimeSpan PreferredEndTime { get; set; } = new TimeSpan(17, 0, 0);  // 17:00
    public List<DayOfWeek> PreferredDays { get; set; } = new List<DayOfWeek>();
    public List<DayOfWeek> ExcludedDays { get; set; } = new List<DayOfWeek>();
    public int MaxDaysFromPreferred { get; set; } = 7;
    public bool PreferMorning { get; set; } = false;
    public bool PreferAfternoon { get; set; } = false;
    public Guid? PreferredRoomId { get; set; }
    public SchedulingPriority Priority { get; set; } = SchedulingPriority.Normal;
}

public class UpdateAppointmentDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RoomId { get; set; }
    public string? ServiceType { get; set; }
    public string? Notes { get; set; }
    public decimal? Price { get; set; }
    public bool IsPaid { get; set; }
}

public class RescheduleAppointmentDto
{
    public DateTime NewStartTime { get; set; }
    public DateTime NewEndTime { get; set; }
    public string? Reason { get; set; }
    public bool UseSmartScheduling { get; set; } = true;
    public SmartSchedulingPreferencesDto? Preferences { get; set; }
}

public class AppointmentSearchDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? RoomId { get; set; }
    public AppointmentStatus? Status { get; set; }
    public string? ServiceType { get; set; }
    public string? SearchTerm { get; set; }
    public bool IncludeCancelled { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AppointmentStatisticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Basic Counts
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }
    public int ScheduledAppointments { get; set; }

    // Rates
    public double CompletionRate { get; set; }
    public double CancellationRate { get; set; }
    public double NoShowRate { get; set; }

    // Revenue
    public decimal TotalRevenue { get; set; }
    public decimal AverageAppointmentValue { get; set; }
    public decimal PendingRevenue { get; set; }

    // Breakdowns
    public Dictionary<string, int> AppointmentsByStatus { get; set; } = new Dictionary<string, int>();
    public Dictionary<Guid, int> AppointmentsByDoctor { get; set; } = new Dictionary<Guid, int>();
    public Dictionary<Guid, int> AppointmentsByRoom { get; set; } = new Dictionary<Guid, int>();
    public Dictionary<int, int> AppointmentsByHour { get; set; } = new Dictionary<int, int>();
    public Dictionary<DayOfWeek, int> AppointmentsByDay { get; set; } = new Dictionary<DayOfWeek, int>();
}

// Smart Scheduling Response DTOs

public class OptimalSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RoomId { get; set; }
    public bool IsAvailable { get; set; }

    // Scoring information
    public double OptimalityScore { get; set; }
    public double UtilizationScore { get; set; }
    public double PreferenceScore { get; set; }

    // Display information
    public string DoctorName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    // Optimization factors
    public List<string> OptimizationFactors { get; set; } = new List<string>();
    public List<string> ConflictReasons { get; set; } = new List<string>();
}

public class ScheduleOptimizationResultDto
{
    public bool Success { get; set; }
    public int AppointmentsOptimized { get; set; }
    public int ConflictsResolved { get; set; }
    public double UtilizationImprovement { get; set; }
    public TimeSpan TimeSaved { get; set; }
    public List<string> Changes { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public DateTime OptimizationDate { get; set; }
    public OptimizationStrategy StrategyUsed { get; set; }
}

public class SchedulingConflictDto
{
    public Guid Id { get; set; }
    public ConflictType Type { get; set; }
    public ConflictSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<Guid> AffectedAppointmentIds { get; set; } = new List<Guid>();
    public DateTime ConflictTime { get; set; }
    public List<string> SuggestedResolutions { get; set; } = new List<string>();
    public bool CanAutoResolve { get; set; }
}

public class UtilizationMetricsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Overall metrics
    public double OverallDoctorUtilization { get; set; }
    public double OverallRoomUtilization { get; set; }

    // Detailed breakdowns
    public Dictionary<Guid, double> DoctorUtilization { get; set; } = new Dictionary<Guid, double>();
    public Dictionary<Guid, double> RoomUtilization { get; set; } = new Dictionary<Guid, double>();
    public Dictionary<int, double> HourlyUtilization { get; set; } = new Dictionary<int, double>();
    public Dictionary<DayOfWeek, double> DailyUtilization { get; set; } = new Dictionary<DayOfWeek, double>();

    // Efficiency metrics
    public double AverageGapBetweenAppointments { get; set; }
    public int TotalWastedTimeMinutes { get; set; }
    public double OptimizationOpportunityScore { get; set; }
}

// AGGIUNGI al file AppointmentDto.cs

public class SchedulingDebugInfo
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime PreferredDate { get; set; }

    // Risorse disponibili
    public int AvailableDoctors { get; set; }
    public int AvailableRooms { get; set; }
    public List<Guid> DoctorIds { get; set; } = new List<Guid>();
    public List<Guid> RoomIds { get; set; } = new List<Guid>();

    // Finestra di ricerca
    public DateTime SearchStartDate { get; set; }
    public DateTime SearchEndDate { get; set; }

    // Appuntamenti esistenti
    public int ExistingAppointmentsInWindow { get; set; }

    // Slot generati
    public int SampleSlotsGenerated { get; set; }
    public List<SlotDebugInfo> SampleSlots { get; set; } = new List<SlotDebugInfo>();

    // Messaggi di debug
    public List<string> DebugMessages { get; set; } = new List<string>();
}

public class SlotDebugInfo
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
}