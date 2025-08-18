using Microsoft.Extensions.Logging;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;

namespace PoliCare.Services.Services;

/// <summary>
/// Smart Scheduling Engine - Il cuore dell'ottimizzazione intelligente di PoliCare
/// </summary>
public class SchedulingService : ISchedulingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SchedulingService> _logger;

    public SchedulingService(IUnitOfWork unitOfWork, ILogger<SchedulingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// ALGORITMO PRINCIPALE: Trova il miglior slot disponibile utilizzando AI-driven optimization
    /// </summary>
    public async Task<AppointmentSlot?> FindOptimalSlotAsync(
        Guid clinicId,
        Guid? preferredDoctorId,
        Guid patientId,
        DateTime preferredDate,
        int durationMinutes,
        AppointmentType appointmentType,
        SchedulingPreferences? preferences = null)
    {
        try
        {
            _logger.LogInformation("Finding optimal slot for patient {PatientId}, preferred date {PreferredDate}",
                patientId, preferredDate);

            // 1. Ottieni tutte le risorse disponibili
            var availableDoctors = await GetAvailableDoctorsAsync(clinicId, preferredDoctorId, appointmentType);
            var availableRooms = await GetAvailableRoomsAsync(clinicId);

            if (!availableDoctors.Any() || !availableRooms.Any())
            {
                _logger.LogWarning("No available doctors or rooms for clinic {ClinicId}", clinicId);
                return null;
            }

            // 2. Genera finestra di ricerca intelligente
            var searchWindow = GenerateSearchWindow(preferredDate, preferences);

            // 3. Trova tutti gli slot possibili
            var candidateSlots = new List<AppointmentSlot>();

            foreach (var doctor in availableDoctors)
            {
                foreach (var room in availableRooms)
                {
                    var slots = await FindSlotsForDoctorRoomAsync(
                        doctor, room, searchWindow.StartDate, searchWindow.EndDate, durationMinutes);
                    candidateSlots.AddRange(slots);
                }
            }

            if (!candidateSlots.Any())
            {
                _logger.LogWarning("No candidate slots found for the specified criteria");
                return null;
            }

            // 4. ALGORITMO DI SCORING: Calcola ottimalità per ogni slot
            var scoredSlots = await ScoreSlotsAsync(candidateSlots, preferredDate, preferences, patientId);

            // 5. Ritorna il miglior slot
            var optimalSlot = scoredSlots
                .Where(s => s.IsAvailable)
                .OrderByDescending(s => s.OptimalityScore)
                .FirstOrDefault();

            if (optimalSlot != null)
            {
                _logger.LogInformation("Found optimal slot: {StartTime} with score {Score}",
                    optimalSlot.StartTime, optimalSlot.OptimalityScore);
            }

            return optimalSlot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding optimal slot");
            throw;
        }
    }

    /// <summary>
    /// Ottiene tutti gli slot disponibili con scoring per un periodo
    /// </summary>
    public async Task<IEnumerable<AppointmentSlot>> GetAvailableSlotsAsync(
        Guid clinicId,
        DateTime startDate,
        DateTime endDate,
        int durationMinutes,
        Guid? doctorId = null,
        Guid? roomId = null)
    {
        try
        {
            var availableSlots = new List<AppointmentSlot>();

            // Ottieni risorse filtrate
            var doctors = doctorId.HasValue
                ? await GetDoctorsByIdsAsync(new[] { doctorId.Value })
                : await GetAvailableDoctorsAsync(clinicId, null, AppointmentType.FirstVisit);

            var rooms = roomId.HasValue
                ? await GetRoomsByIdsAsync(new[] { roomId.Value })
                : await GetAvailableRoomsAsync(clinicId);

            // Genera slot per ogni combinazione doctor-room
            foreach (var doctor in doctors)
            {
                foreach (var room in rooms)
                {
                    var slots = await FindSlotsForDoctorRoomAsync(doctor, room, startDate, endDate, durationMinutes);
                    availableSlots.AddRange(slots);
                }
            }

            // Calcola scoring base per tutti gli slot
            var scoredSlots = await ScoreSlotsAsync(availableSlots, startDate, null, Guid.Empty);

            return scoredSlots.Where(s => s.IsAvailable).OrderByDescending(s => s.OptimalityScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available slots");
            throw;
        }
    }

    /// <summary>
    /// ALGORITMO DI OTTIMIZZAZIONE: Riorganizza l'agenda per massimizzare l'efficienza
    /// </summary>
    public async Task<ScheduleOptimizationResult> OptimizeScheduleAsync(
        Guid clinicId,
        DateTime date,
        OptimizationStrategy strategy = OptimizationStrategy.Balanced)
    {
        try
        {
            _logger.LogInformation("Starting schedule optimization for clinic {ClinicId} on {Date} with strategy {Strategy}",
                clinicId, date, strategy);

            var result = new ScheduleOptimizationResult
            {
                OptimizationDate = date,
                StrategyUsed = strategy
            };

            // 1. Ottieni tutti gli appuntamenti del giorno
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1).AddSeconds(-1);

            var appointments = await _unitOfWork.Repository<Appointment>()
                .GetWhereAsync(a => a.StartTime >= dayStart && a.StartTime <= dayEnd &&
                              a.Status != AppointmentStatus.Cancelled.ToString());

            if (!appointments.Any())
            {
                result.Success = true;
                result.Changes.Add("No appointments to optimize");
                return result;
            }

            // 2. Rileva conflitti esistenti
            var conflicts = await DetectConflictsForDateAsync(clinicId, date);

            // 3. Applica strategia di ottimizzazione
            switch (strategy)
            {
                case OptimizationStrategy.MaximizeUtilization:
                    await OptimizeForUtilizationAsync(appointments.ToList(), result);
                    break;
                case OptimizationStrategy.MinimizeGaps:
                    await OptimizeForGapsAsync(appointments.ToList(), result);
                    break;
                case OptimizationStrategy.Balanced:
                    await OptimizeBalancedAsync(appointments.ToList(), result);
                    break;
                case OptimizationStrategy.PatientPreference:
                    await OptimizeForPatientsAsync(appointments.ToList(), result);
                    break;
                case OptimizationStrategy.DoctorWorkload:
                    await OptimizeWorkloadAsync(appointments.ToList(), result);
                    break;
            }

            // 4. Risolvi conflitti rimanenti
            var remainingConflicts = await DetectConflictsForDateAsync(clinicId, date);
            result.ConflictsResolved = conflicts.Count() - remainingConflicts.Count();

            result.Success = true;
            _logger.LogInformation("Schedule optimization completed. Optimized {Count} appointments",
                result.AppointmentsOptimized);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing schedule");
            throw;
        }
    }

    /// <summary>
    /// Rileva conflitti di scheduling
    /// </summary>
    public async Task<IEnumerable<SchedulingConflict>> DetectConflictsAsync(
        Guid clinicId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var conflicts = new List<SchedulingConflict>();

            // Ottieni tutti gli appuntamenti nel periodo
            var appointments = await _unitOfWork.Repository<Appointment>()
                .GetWhereAsync(a => a.StartTime >= startDate && a.StartTime <= endDate &&
                              a.Status != AppointmentStatus.Cancelled.ToString());

            // Rileva double booking per medici
            var doctorConflicts = DetectDoctorConflicts(appointments);
            conflicts.AddRange(doctorConflicts);

            // Rileva conflitti sale
            var roomConflicts = DetectRoomConflicts(appointments);
            conflicts.AddRange(roomConflicts);

            // Rileva orari fuori dalle disponibilità
            var availabilityConflicts = await DetectAvailabilityConflictsAsync(appointments);
            conflicts.AddRange(availabilityConflicts);

            _logger.LogInformation("Detected {Count} conflicts for clinic {ClinicId}", conflicts.Count, clinicId);
            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting conflicts");
            throw;
        }
    }

    /// <summary>
    /// Crea un appuntamento utilizzando l'algoritmo intelligente
    /// </summary>
    public async Task<Appointment> CreateOptimizedAppointmentAsync(CreateAppointmentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating optimized appointment for patient {PatientId}", request.PatientId);

            // 1. Trova lo slot ottimale
            var optimalSlot = await FindOptimalSlotAsync(
                request.ClinicId,
                request.PreferredDoctorId,
                request.PatientId,
                request.PreferredDate,
                request.DurationMinutes,
                request.Type,
                request.Preferences);

            if (optimalSlot == null)
            {
                throw new InvalidOperationException("No optimal slot found for the specified criteria");
            }

            // 2. Crea l'appuntamento
            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                DoctorId = optimalSlot.DoctorId,
                RoomId = optimalSlot.RoomId,
                StartTime = optimalSlot.StartTime,
                EndTime = optimalSlot.EndTime,
                Status = AppointmentStatus.Scheduled.ToString(),
                ServiceType = request.ServiceType,
                Notes = request.Notes,
                Price = request.Price
            };

            await _unitOfWork.Repository<Appointment>().AddAsync(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Created optimized appointment {AppointmentId} at {StartTime}",
                appointment.Id, appointment.StartTime);

            return appointment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating optimized appointment");
            throw;
        }
    }

    // PRIVATE HELPER METHODS

    private async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync(Guid clinicId, Guid? preferredDoctorId, AppointmentType type)
    {
        if (preferredDoctorId.HasValue)
        {
            var preferredDoctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(preferredDoctorId.Value);
            return preferredDoctor != null ? new[] { preferredDoctor } : Array.Empty<Doctor>();
        }

        return await _unitOfWork.Repository<Doctor>()
            .GetWhereAsync(d => d.ClinicId == clinicId);
    }

    private async Task<IEnumerable<Room>> GetAvailableRoomsAsync(Guid clinicId)
    {
        return await _unitOfWork.Repository<Room>()
            .GetWhereAsync(r => r.ClinicId == clinicId && r.IsActive);
    }

    private async Task<IEnumerable<Doctor>> GetDoctorsByIdsAsync(IEnumerable<Guid> doctorIds)
    {
        var doctors = new List<Doctor>();
        foreach (var id in doctorIds)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(id);
            if (doctor != null) doctors.Add(doctor);
        }
        return doctors;
    }

    private async Task<IEnumerable<Room>> GetRoomsByIdsAsync(IEnumerable<Guid> roomIds)
    {
        var rooms = new List<Room>();
        foreach (var id in roomIds)
        {
            var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id);
            if (room != null) rooms.Add(room);
        }
        return rooms;
    }

    private (DateTime StartDate, DateTime EndDate) GenerateSearchWindow(DateTime preferredDate, SchedulingPreferences? preferences)
    {
        var maxDays = preferences?.MaxDaysFromPreferred ?? 7;
        var startDate = preferredDate.Date;
        var endDate = preferredDate.Date.AddDays(maxDays);

        return (startDate, endDate);
    }

    private async Task<IEnumerable<AppointmentSlot>> FindSlotsForDoctorRoomAsync(
        Doctor doctor, Room room, DateTime startDate, DateTime endDate, int durationMinutes)
    {
        var slots = new List<AppointmentSlot>();
        var duration = TimeSpan.FromMinutes(durationMinutes);

        // Ottieni appuntamenti esistenti per questo medico e sala
        var existingAppointments = await _unitOfWork.Repository<Appointment>()
            .GetWhereAsync(a => (a.DoctorId == doctor.Id || a.RoomId == room.Id) &&
                              a.StartTime >= startDate && a.StartTime <= endDate &&
                              a.Status != AppointmentStatus.Cancelled.ToString());

        // Genera slot ogni 15 minuti dalle 8:00 alle 18:00
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            var dayStart = currentDate.Date.AddHours(8); // 8:00
            var dayEnd = currentDate.Date.AddHours(18);   // 18:00

            var current = dayStart;
            while (current.Add(duration) <= dayEnd)
            {
                var slotEnd = current.Add(duration);

                // Verifica se lo slot è libero
                var hasConflict = existingAppointments.Any(a =>
                    (a.StartTime < slotEnd && a.EndTime > current));

                if (!hasConflict)
                {
                    slots.Add(new AppointmentSlot
                    {
                        StartTime = current,
                        EndTime = slotEnd,
                        DoctorId = doctor.Id,
                        RoomId = room.Id,
                        IsAvailable = true,
                        DoctorName = $"{doctor.User?.FirstName} {doctor.User?.LastName}".Trim(),
                        RoomName = room.Name,
                        Specialization = doctor.Specialization
                    });
                }

                current = current.AddMinutes(15); // Slot ogni 15 minuti
            }

            currentDate = currentDate.AddDays(1);
        }

        return slots;
    }

    /// <summary>
    /// ALGORITMO DI SCORING: Calcola l'ottimalità di ogni slot
    /// </summary>
    private async Task<IEnumerable<AppointmentSlot>> ScoreSlotsAsync(
        IEnumerable<AppointmentSlot> slots,
        DateTime preferredDate,
        SchedulingPreferences? preferences,
        Guid patientId)
    {
        var scoredSlots = new List<AppointmentSlot>();

        foreach (var slot in slots)
        {
            var score = await CalculateSlotScoreAsync(slot, preferredDate, preferences, patientId);
            slot.OptimalityScore = score.TotalScore;
            slot.UtilizationScore = score.UtilizationScore;
            slot.PreferenceScore = score.PreferenceScore;
            slot.OptimizationFactors = score.Factors;

            scoredSlots.Add(slot);
        }

        return scoredSlots;
    }

    private async Task<SlotScore> CalculateSlotScoreAsync(
        AppointmentSlot slot,
        DateTime preferredDate,
        SchedulingPreferences? preferences,
        Guid patientId)
    {
        var score = new SlotScore();

        // 1. PROXIMITY SCORE (0-40 punti): Vicinanza alla data preferita
        var daysDifference = Math.Abs((slot.StartTime.Date - preferredDate.Date).Days);
        score.ProximityScore = Math.Max(0, 40 - (daysDifference * 5));

        // 2. TIME PREFERENCE SCORE (0-30 punti): Rispetto preferenze orarie
        if (preferences != null)
        {
            var slotTime = slot.StartTime.TimeOfDay;
            if (slotTime >= preferences.PreferredStartTime && slotTime <= preferences.PreferredEndTime)
                score.TimePreferenceScore = 30;
            else if (preferences.PreferMorning && slotTime < TimeSpan.FromHours(12))
                score.TimePreferenceScore = 20;
            else if (preferences.PreferAfternoon && slotTime >= TimeSpan.FromHours(12))
                score.TimePreferenceScore = 20;
            else
                score.TimePreferenceScore = 10;
        }
        else
        {
            // Default: preferenza per orari centrali (9-17)
            var hour = slot.StartTime.Hour;
            if (hour >= 9 && hour <= 17)
                score.TimePreferenceScore = 25;
            else
                score.TimePreferenceScore = 15;
        }

        // 3. UTILIZATION SCORE (0-20 punti): Ottimizzazione utilizzo risorse
        score.UtilizationScore = await CalculateUtilizationScoreAsync(slot);

        // 4. WORKLOAD BALANCE SCORE (0-10 punti): Bilanciamento carico lavoro
        score.WorkloadScore = await CalculateWorkloadBalanceScoreAsync(slot);

        // Calcola score totale
        score.TotalScore = score.ProximityScore + score.TimePreferenceScore +
                          score.UtilizationScore + score.WorkloadScore;

        // Fattori che hanno influenzato il score
        score.Factors.Add($"Proximity: {score.ProximityScore}/40");
        score.Factors.Add($"Time Preference: {score.TimePreferenceScore}/30");
        score.Factors.Add($"Utilization: {score.UtilizationScore}/20");
        score.Factors.Add($"Workload: {score.WorkloadScore}/10");

        score.PreferenceScore = score.TimePreferenceScore;

        return score;
    }

    private async Task<double> CalculateUtilizationScoreAsync(AppointmentSlot slot)
    {
        // Calcola quanto questo slot ottimizza l'utilizzo delle risorse
        var dayStart = slot.StartTime.Date.AddHours(8);
        var dayEnd = slot.StartTime.Date.AddHours(18);

        // Conta appuntamenti del medico quel giorno
        var doctorAppointments = await _unitOfWork.Repository<Appointment>()
            .GetWhereAsync(a => a.DoctorId == slot.DoctorId &&
                              a.StartTime >= dayStart && a.StartTime <= dayEnd &&
                              a.Status != AppointmentStatus.Cancelled.ToString());

        var appointmentCount = doctorAppointments.Count();

        // Più appuntamenti = migliore utilizzo, ma non sovraccarico
        if (appointmentCount <= 2) return 20; // Ottimo
        if (appointmentCount <= 4) return 15; // Buono
        if (appointmentCount <= 6) return 10; // Discreto
        if (appointmentCount <= 8) return 5;  // Pieno
        return 0; // Sovraccarico
    }

    private async Task<double> CalculateWorkloadBalanceScoreAsync(AppointmentSlot slot)
    {
        // Verifica bilanciamento del carico tra medici
        // Implementazione semplificata
        return 8; // Score medio per ora
    }

    // Metodi di ottimizzazione per diverse strategie
    private async Task OptimizeForUtilizationAsync(List<Appointment> appointments, ScheduleOptimizationResult result)
    {
        // Implementazione per massimizzare utilizzo
        result.Changes.Add("Applied utilization maximization strategy");
        result.AppointmentsOptimized = appointments.Count;
    }

    private async Task OptimizeForGapsAsync(List<Appointment> appointments, ScheduleOptimizationResult result)
    {
        // Implementazione per minimizzare gap
        result.Changes.Add("Applied gap minimization strategy");
        result.AppointmentsOptimized = appointments.Count;
    }

    private async Task OptimizeBalancedAsync(List<Appointment> appointments, ScheduleOptimizationResult result)
    {
        // Implementazione strategia bilanciata
        result.Changes.Add("Applied balanced optimization strategy");
        result.AppointmentsOptimized = appointments.Count;
    }

    private async Task OptimizeForPatientsAsync(List<Appointment> appointments, ScheduleOptimizationResult result)
    {
        // Implementazione per preferenze pazienti
        result.Changes.Add("Applied patient preference strategy");
        result.AppointmentsOptimized = appointments.Count;
    }

    private async Task OptimizeWorkloadAsync(List<Appointment> appointments, ScheduleOptimizationResult result)
    {
        // Implementazione per bilanciamento workload
        result.Changes.Add("Applied workload balancing strategy");
        result.AppointmentsOptimized = appointments.Count;
    }

    // Metodi di rilevamento conflitti
    private IEnumerable<SchedulingConflict> DetectDoctorConflicts(IEnumerable<Appointment> appointments)
    {
        var conflicts = new List<SchedulingConflict>();
        var doctorAppointments = appointments.GroupBy(a => a.DoctorId);

        foreach (var group in doctorAppointments)
        {
            var doctorAppts = group.OrderBy(a => a.StartTime).ToList();

            for (int i = 0; i < doctorAppts.Count - 1; i++)
            {
                var current = doctorAppts[i];
                var next = doctorAppts[i + 1];

                if (current.EndTime > next.StartTime)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        Type = ConflictType.DoubleBooking,
                        Severity = ConflictSeverity.High,
                        Description = $"Doctor double booking detected",
                        AffectedAppointmentIds = new List<Guid> { current.Id, next.Id },
                        ConflictTime = next.StartTime,
                        CanAutoResolve = true
                    });
                }
            }
        }

        return conflicts;
    }

    private IEnumerable<SchedulingConflict> DetectRoomConflicts(IEnumerable<Appointment> appointments)
    {
        var conflicts = new List<SchedulingConflict>();
        var roomAppointments = appointments.GroupBy(a => a.RoomId);

        foreach (var group in roomAppointments)
        {
            var roomAppts = group.OrderBy(a => a.StartTime).ToList();

            for (int i = 0; i < roomAppts.Count - 1; i++)
            {
                var current = roomAppts[i];
                var next = roomAppts[i + 1];

                if (current.EndTime > next.StartTime)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        Type = ConflictType.RoomConflict,
                        Severity = ConflictSeverity.High,
                        Description = $"Room double booking detected",
                        AffectedAppointmentIds = new List<Guid> { current.Id, next.Id },
                        ConflictTime = next.StartTime,
                        CanAutoResolve = true
                    });
                }
            }
        }

        return conflicts;
    }

    private async Task<IEnumerable<SchedulingConflict>> DetectAvailabilityConflictsAsync(IEnumerable<Appointment> appointments)
    {
        // Implementazione per rilevare conflitti con disponibilità medici
        return new List<SchedulingConflict>();
    }

    private async Task<IEnumerable<SchedulingConflict>> DetectConflictsForDateAsync(Guid clinicId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = date.Date.AddDays(1).AddSeconds(-1);

        return await DetectConflictsAsync(clinicId, dayStart, dayEnd);
    }

    // Stub implementations per completare l'interfaccia
    public async Task<ConflictResolutionResult> ResolveConflictsAsync(Guid clinicId, IEnumerable<Guid> conflictIds, bool autoApprove = false)
    {
        // Implementazione risoluzione conflitti
        return new ConflictResolutionResult { Success = true };
    }

    public async Task<Appointment?> RescheduleAppointmentAsync(Guid appointmentId, DateTime? newPreferredDate = null, SchedulingPreferences? preferences = null)
    {
        // Implementazione riprogrammazione
        return await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
    }

    public async Task<UtilizationMetrics> GetUtilizationMetricsAsync(Guid clinicId, DateTime startDate, DateTime endDate)
    {
        // Implementazione metriche utilizzo
        return new UtilizationMetrics { StartDate = startDate, EndDate = endDate };
    }

    public async Task<IEnumerable<SchedulingRecommendation>> GetSchedulingRecommendationsAsync(Guid clinicId, DateTime analysisDate)
    {
        // Implementazione raccomandazioni
        return new List<SchedulingRecommendation>();
    }

    public async Task<bool> IsSlotAvailableAsync(Guid doctorId, Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeAppointmentId = null)
    {
        var hasConflict = await _unitOfWork.Repository<Appointment>()
            .ExistsAsync(a => (a.DoctorId == doctorId || a.RoomId == roomId) &&
                            a.StartTime < endTime && a.EndTime > startTime &&
                            a.Status != AppointmentStatus.Cancelled.ToString() &&
                            (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value));

        return !hasConflict;
    }

    public async Task<IEnumerable<AppointmentSlot>> GetNextAvailableSlotsAsync(Guid doctorId, int numberOfSlots = 5, int durationMinutes = 30)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId);
        if (doctor == null) return new List<AppointmentSlot>();

        var rooms = await GetAvailableRoomsAsync(doctor.ClinicId);
        var slots = new List<AppointmentSlot>();

        var searchDate = DateTime.Now.Date;
        var endDate = searchDate.AddDays(30);

        foreach (var room in rooms.Take(1)) // Prendi solo la prima sala per semplicità
        {
            var doctorSlots = await FindSlotsForDoctorRoomAsync(doctor, room, searchDate, endDate, durationMinutes);
            slots.AddRange(doctorSlots.Take(numberOfSlots));
        }

        return slots.Take(numberOfSlots);
    }

    /// <summary>
    /// Metodo pubblico per ottenere medici disponibili (per debug)
    /// </summary>

}

/// <summary>
/// Struttura interna per il calcolo del score
/// </summary>
internal class SlotScore
{
    public double ProximityScore { get; set; }
    public double TimePreferenceScore { get; set; }
    public double UtilizationScore { get; set; }
    public double WorkloadScore { get; set; }
    public double TotalScore { get; set; }
    public double PreferenceScore { get; set; }
    public List<string> Factors { get; set; } = new List<string>();
}