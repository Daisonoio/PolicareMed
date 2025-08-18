using PoliCare.Core.Entities;

namespace PoliCare.Services.Interfaces;

/// <summary>
/// Servizio per la gestione completa degli appuntamenti
/// Integrato con Smart Scheduling Engine
/// </summary>
public interface IAppointmentService
{
    // BASIC CRUD OPERATIONS
    Task<IEnumerable<Appointment>> GetAppointmentsByClinicAsync(Guid clinicId, DateTime? date = null);
    Task<IEnumerable<Appointment>> GetAppointmentsByDoctorAsync(Guid doctorId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Appointment>> GetAppointmentsByPatientAsync(Guid patientId);
    Task<IEnumerable<Appointment>> GetAppointmentsByRoomAsync(Guid roomId, DateTime date);
    Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId);

    // SMART APPOINTMENT CREATION
    Task<Appointment> CreateAppointmentAsync(Appointment appointment);
    Task<Appointment> CreateSmartAppointmentAsync(CreateAppointmentRequest request);

    // APPOINTMENT MANAGEMENT
    Task<Appointment?> UpdateAppointmentAsync(Guid appointmentId, Appointment appointment);
    Task<bool> CancelAppointmentAsync(Guid appointmentId, string reason);
    Task<Appointment?> RescheduleAppointmentAsync(Guid appointmentId, DateTime newStartTime, DateTime newEndTime);
    Task<bool> ConfirmAppointmentAsync(Guid appointmentId);
    Task<bool> CompleteAppointmentAsync(Guid appointmentId, string? notes = null);
    Task<bool> MarkNoShowAsync(Guid appointmentId);

    // STATUS AND VALIDATION
    Task<bool> ValidateAppointmentAsync(Appointment appointment);
    Task<IEnumerable<string>> GetValidationErrorsAsync(Appointment appointment);
    Task<bool> CanCancelAppointmentAsync(Guid appointmentId);
    Task<bool> CanRescheduleAppointmentAsync(Guid appointmentId);

    // SEARCH AND FILTERING
    Task<IEnumerable<Appointment>> SearchAppointmentsAsync(Guid clinicId, AppointmentSearchCriteria criteria);
    Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(Guid clinicId, AppointmentStatus status, DateTime? date = null);
    Task<IEnumerable<Appointment>> GetTodayAppointmentsAsync(Guid clinicId);
    Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid clinicId, int days = 7);

    // ANALYTICS
    Task<AppointmentStatistics> GetAppointmentStatisticsAsync(Guid clinicId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Appointment>> GetConflictingAppointmentsAsync(Guid clinicId, DateTime date);
}

/// <summary>
/// Criteri di ricerca per appuntamenti
/// </summary>
public class AppointmentSearchCriteria
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? RoomId { get; set; }
    public AppointmentStatus? Status { get; set; }
    public string? ServiceType { get; set; }
    public string? SearchTerm { get; set; } // Nome paziente, note, etc.
    public bool IncludeCancelled { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Statistiche appuntamenti per analytics
/// </summary>
public class AppointmentStatistics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // BASIC COUNTS
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }
    public int ScheduledAppointments { get; set; }

    // RATES
    public double CompletionRate { get; set; } // %
    public double CancellationRate { get; set; } // %
    public double NoShowRate { get; set; } // %

    // BY STATUS
    public Dictionary<string, int> AppointmentsByStatus { get; set; } = new Dictionary<string, int>();

    // BY DOCTOR
    public Dictionary<Guid, int> AppointmentsByDoctor { get; set; } = new Dictionary<Guid, int>();

    // BY ROOM
    public Dictionary<Guid, int> AppointmentsByRoom { get; set; } = new Dictionary<Guid, int>();

    // TIME ANALYTICS
    public Dictionary<int, int> AppointmentsByHour { get; set; } = new Dictionary<int, int>(); // 0-23
    public Dictionary<DayOfWeek, int> AppointmentsByDay { get; set; } = new Dictionary<DayOfWeek, int>();

    // REVENUE
    public decimal TotalRevenue { get; set; }
    public decimal AverageAppointmentValue { get; set; }
    public decimal PendingRevenue { get; set; } // Non ancora pagati
}