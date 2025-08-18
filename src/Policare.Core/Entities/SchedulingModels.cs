namespace PoliCare.Core.Entities;

/// <summary>
/// Preferenze per l'algoritmo di scheduling
/// </summary>
public class SchedulingPreferences
{
    public TimeSpan PreferredStartTime { get; set; }
    public TimeSpan PreferredEndTime { get; set; }
    public List<DayOfWeek> PreferredDays { get; set; } = new List<DayOfWeek>();
    public List<DayOfWeek> ExcludedDays { get; set; } = new List<DayOfWeek>();
    public int MaxDaysFromPreferred { get; set; } = 7;
    public bool PreferMorning { get; set; } = false;
    public bool PreferAfternoon { get; set; } = false;
    public Guid? PreferredRoomId { get; set; }
    public SchedulingPriority Priority { get; set; } = SchedulingPriority.Normal;
}

/// <summary>
/// Richiesta per creare un appuntamento ottimizzato
/// </summary>
public class CreateAppointmentRequest
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
    public SchedulingPreferences? Preferences { get; set; }
    public bool AutoOptimize { get; set; } = true;
}

/// <summary>
/// Risultato dell'ottimizzazione dell'agenda
/// </summary>
public class ScheduleOptimizationResult
{
    public bool Success { get; set; }
    public int AppointmentsOptimized { get; set; }
    public int ConflictsResolved { get; set; }
    public double UtilizationImprovement { get; set; } // Percentuale di miglioramento
    public TimeSpan TimeSaved { get; set; }
    public List<string> Changes { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public DateTime OptimizationDate { get; set; }
    public OptimizationStrategy StrategyUsed { get; set; }
}

/// <summary>
/// Conflitto di scheduling rilevato
/// </summary>
public class SchedulingConflict
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ConflictType Type { get; set; }
    public ConflictSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<Guid> AffectedAppointmentIds { get; set; } = new List<Guid>();
    public List<Guid> AffectedResourceIds { get; set; } = new List<Guid>();
    public DateTime ConflictTime { get; set; }
    public List<string> SuggestedResolutions { get; set; } = new List<string>();
    public bool CanAutoResolve { get; set; }
}

/// <summary>
/// Risultato della risoluzione dei conflitti
/// </summary>
public class ConflictResolutionResult
{
    public bool Success { get; set; }
    public int ConflictsResolved { get; set; }
    public int ConflictsRemaining { get; set; }
    public List<Guid> ResolvedConflictIds { get; set; } = new List<Guid>();
    public List<Guid> UnresolvedConflictIds { get; set; } = new List<Guid>();
    public List<string> Actions { get; set; } = new List<string>();
    public List<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Metriche di utilizzo risorse
/// </summary>
public class UtilizationMetrics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // DOCTOR METRICS
    public double OverallDoctorUtilization { get; set; } // 0-100%
    public Dictionary<Guid, double> DoctorUtilization { get; set; } = new Dictionary<Guid, double>();
    public Dictionary<Guid, int> DoctorAppointmentCount { get; set; } = new Dictionary<Guid, int>();

    // ROOM METRICS
    public double OverallRoomUtilization { get; set; } // 0-100%
    public Dictionary<Guid, double> RoomUtilization { get; set; } = new Dictionary<Guid, double>();
    public Dictionary<Guid, int> RoomAppointmentCount { get; set; } = new Dictionary<Guid, int>();

    // TIME METRICS
    public Dictionary<int, double> HourlyUtilization { get; set; } = new Dictionary<int, double>(); // 0-23
    public Dictionary<DayOfWeek, double> DailyUtilization { get; set; } = new Dictionary<DayOfWeek, double>();

    // EFFICIENCY METRICS
    public double AverageGapBetweenAppointments { get; set; } // In minutes
    public int TotalWastedTimeMinutes { get; set; }
    public double OptimizationOpportunityScore { get; set; } // 0-100
}

/// <summary>
/// Raccomandazione per migliorare lo scheduling
/// </summary>
public class SchedulingRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RecommendationType Type { get; set; }
    public RecommendationPriority Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
    public double PotentialImprovement { get; set; } // Percentuale di miglioramento stimata
    public List<Guid> AffectedResources { get; set; } = new List<Guid>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

// ENUMS

public enum OptimizationStrategy
{
    MaximizeUtilization,    // Massimizza l'utilizzo delle risorse
    MinimizeGaps,          // Minimizza i tempi morti
    Balanced,              // Bilanciato tra efficienza e flessibilità
    PatientPreference,     // Priorità alle preferenze pazienti
    DoctorWorkload        // Bilancia il carico di lavoro tra medici
}

public enum ConflictType
{
    DoubleBooking,         // Doppia prenotazione
    RoomConflict,          // Conflitto sala
    DoctorUnavailable,     // Medico non disponibile
    OutsideWorkingHours,   // Fuori orario lavorativo
    InsufficientTime,      // Tempo insufficiente
    ResourceOverallocation // Sovra-allocazione risorse
}

public enum ConflictSeverity
{
    Low,       // Risoluzione consigliata ma non urgente
    Medium,    // Richiede attenzione
    High,      // Risoluzione necessaria
    Critical   // Blocca il sistema
}

public enum SchedulingPriority
{
    Low,
    Normal,
    High,
    Urgent,
    Emergency
}

public enum RecommendationType
{
    TimeSlotOptimization,  // Ottimizzazione slot temporali
    ResourceReallocation,  // Riallocazione risorse
    WorkloadBalancing,     // Bilanciamento carico lavoro
    CapacityExpansion,     // Espansione capacità
    ScheduleRestructuring  // Ristrutturazione agenda
}

public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}