using PoliCare.Core.Entities;

namespace PoliCare.Services.Interfaces;

/// <summary>
/// Smart Scheduling Engine - La funzionalità differenziante di PoliCare
/// Gestisce l'ottimizzazione intelligente di agende, sale e risorse
/// </summary>
public interface ISchedulingService
{
    // CORE SCHEDULING FUNCTIONS

    /// <summary>
    /// Trova il miglior slot disponibile per un appuntamento utilizzando l'algoritmo di ottimizzazione
    /// </summary>
    Task<AppointmentSlot?> FindOptimalSlotAsync(
        Guid clinicId,
        Guid? preferredDoctorId,
        Guid patientId,
        DateTime preferredDate,
        int durationMinutes,
        AppointmentType appointmentType,
        SchedulingPreferences? preferences = null);

    /// <summary>
    /// Ottiene tutti gli slot disponibili per un periodo con scoring di ottimalità
    /// </summary>
    Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsAsync(
        Guid clinicId,
        DateTime startDate,
        DateTime endDate,
        int durationMinutes,
        Guid? doctorId = null,
        Guid? roomId = null);

    /// <summary>
    /// Ottimizza automaticamente l'agenda di una clinica per una data specifica
    /// </summary>
    Task<ScheduleOptimizationResult> OptimizeScheduleAsync(
        Guid clinicId,
        DateTime date,
        OptimizationStrategy strategy = OptimizationStrategy.Balanced);

    // CONFLICT MANAGEMENT

    /// <summary>
    /// Rileva tutti i conflitti di scheduling in un periodo
    /// </summary>
    Task<IEnumerable<SchedulingConflict>> DetectConflictsAsync(
        Guid clinicId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Risolve automaticamente i conflitti quando possibile
    /// </summary>
    Task<ConflictResolutionResult> ResolveConflictsAsync(
        Guid clinicId,
        IEnumerable<Guid> conflictIds,
        bool autoApprove = false);

    // APPOINTMENT MANAGEMENT

    /// <summary>
    /// Crea un appuntamento utilizzando l'algoritmo di scheduling intelligente
    /// </summary>
    Task<Appointment> CreateOptimizedAppointmentAsync(
        CreateAppointmentRequest request);

    /// <summary>
    /// Riprogramma un appuntamento esistente ottimizzando la nuova collocazione
    /// </summary>
    Task<Appointment?> RescheduleAppointmentAsync(
        Guid appointmentId,
        DateTime? newPreferredDate = null,
        SchedulingPreferences? preferences = null);

    // ANALYTICS & REPORTING

    /// <summary>
    /// Calcola metriche di utilizzo per medici e sale
    /// </summary>
    Task<UtilizationMetrics> GetUtilizationMetricsAsync(
        Guid clinicId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Suggerisce miglioramenti per l'organizzazione delle agende
    /// </summary>
    Task<IEnumerable<SchedulingRecommendation>> GetSchedulingRecommendationsAsync(
        Guid clinicId,
        DateTime analysisDate);

    // AVAILABILITY MANAGEMENT

    /// <summary>
    /// Verifica disponibilità completa considerando medico, sala e altri fattori
    /// </summary>
    Task<bool> IsSlotAvailableAsync(
        Guid doctorId,
        Guid roomId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeAppointmentId = null);

    /// <summary>
    /// Ottiene i prossimi slot disponibili per un medico specifico
    /// </summary>
    Task<IEnumerable<AppointmentSlot>> GetNextAvailableSlotsAsync(
        Guid doctorId,
        int numberOfSlots = 5,
        int durationMinutes = 30);
}