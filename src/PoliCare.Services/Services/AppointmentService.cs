using Microsoft.Extensions.Logging;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;

namespace PoliCare.Services.Services;

/// <summary>
/// Servizio completo per la gestione degli appuntamenti
/// Integrato con Smart Scheduling Engine
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IUnitOfWork unitOfWork,
        ISchedulingService schedulingService,
        ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    // BASIC CRUD OPERATIONS

    public async Task<IEnumerable<Appointment>> GetAppointmentsByClinicAsync(Guid clinicId, DateTime? date = null)
    {
        try
        {
            if (date.HasValue)
            {
                var dayStart = date.Value.Date;
                var dayEnd = date.Value.Date.AddDays(1).AddSeconds(-1);

                var appointments = await _unitOfWork.Repository<Appointment>()
                    .GetWhereAsync(a => a.StartTime >= dayStart && a.StartTime <= dayEnd);

                // Filtra per clinica attraverso Doctor.ClinicId
                var clinicAppointments = new List<Appointment>();
                foreach (var appointment in appointments)
                {
                    var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(appointment.DoctorId);
                    if (doctor?.ClinicId == clinicId)
                    {
                        clinicAppointments.Add(appointment);
                    }
                }

                return clinicAppointments.OrderBy(a => a.StartTime);
            }
            else
            {
                // Tutti gli appuntamenti della clinica
                var doctors = await _unitOfWork.Repository<Doctor>()
                    .GetWhereAsync(d => d.ClinicId == clinicId);

                var allAppointments = new List<Appointment>();
                foreach (var doctor in doctors)
                {
                    var doctorAppointments = await _unitOfWork.Repository<Appointment>()
                        .GetWhereAsync(a => a.DoctorId == doctor.Id);
                    allAppointments.AddRange(doctorAppointments);
                }

                return allAppointments.OrderBy(a => a.StartTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByDoctorAsync(Guid doctorId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var appointments = await _unitOfWork.Repository<Appointment>()
                .GetWhereAsync(a => a.DoctorId == doctorId &&
                              a.StartTime >= startDate && a.StartTime <= endDate);

            return appointments.OrderBy(a => a.StartTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientAsync(Guid patientId)
    {
        try
        {
            var appointments = await _unitOfWork.Repository<Appointment>()
                .GetWhereAsync(a => a.PatientId == patientId);

            return appointments.OrderByDescending(a => a.StartTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByRoomAsync(Guid roomId, DateTime date)
    {
        try
        {
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1).AddSeconds(-1);

            var appointments = await _unitOfWork.Repository<Appointment>()
                .GetWhereAsync(a => a.RoomId == roomId &&
                              a.StartTime >= dayStart && a.StartTime <= dayEnd);

            return appointments.OrderBy(a => a.StartTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for room {RoomId}", roomId);
            throw;
        }
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId)
    {
        try
        {
            return await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    // SMART APPOINTMENT CREATION

    public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
    {
        try
        {
            // Validazione base
            var validationErrors = await GetValidationErrorsAsync(appointment);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            await _unitOfWork.Repository<Appointment>().AddAsync(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Created appointment {AppointmentId} for patient {PatientId}",
                appointment.Id, appointment.PatientId);

            return appointment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            throw;
        }
    }

    public async Task<Appointment> CreateSmartAppointmentAsync(CreateAppointmentRequest request)
    {
        try
        {
            // Utilizza il Smart Scheduling Engine
            return await _schedulingService.CreateOptimizedAppointmentAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating smart appointment");
            throw;
        }
    }

    // APPOINTMENT MANAGEMENT

    public async Task<Appointment?> UpdateAppointmentAsync(Guid appointmentId, Appointment updatedAppointment)
    {
        try
        {
            var existingAppointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (existingAppointment == null)
            {
                return null;
            }

            // Validazione aggiornamento
            var validationErrors = await GetValidationErrorsAsync(updatedAppointment);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Aggiorna proprietà
            existingAppointment.StartTime = updatedAppointment.StartTime;
            existingAppointment.EndTime = updatedAppointment.EndTime;
            existingAppointment.DoctorId = updatedAppointment.DoctorId;
            existingAppointment.RoomId = updatedAppointment.RoomId;
            existingAppointment.ServiceType = updatedAppointment.ServiceType;
            existingAppointment.Notes = updatedAppointment.Notes;
            existingAppointment.Price = updatedAppointment.Price;

            _unitOfWork.Repository<Appointment>().Update(existingAppointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Updated appointment {AppointmentId}", appointmentId);
            return existingAppointment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    public async Task<bool> CancelAppointmentAsync(Guid appointmentId, string reason)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            // Verifica se può essere cancellato
            if (!await CanCancelAppointmentAsync(appointmentId))
            {
                throw new InvalidOperationException("Appointment cannot be cancelled at this time");
            }

            appointment.Status = AppointmentStatus.Cancelled.ToString();
            appointment.Notes = $"{appointment.Notes ?? ""}\nCancelled: {reason}".Trim();

            _unitOfWork.Repository<Appointment>().Update(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Cancelled appointment {AppointmentId}. Reason: {Reason}", appointmentId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    public async Task<Appointment?> RescheduleAppointmentAsync(Guid appointmentId, DateTime newStartTime, DateTime newEndTime)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return null;
            }

            // Verifica se può essere riprogrammato
            if (!await CanRescheduleAppointmentAsync(appointmentId))
            {
                throw new InvalidOperationException("Appointment cannot be rescheduled at this time");
            }

            // Verifica disponibilità del nuovo slot
            var isAvailable = await _schedulingService.IsSlotAvailableAsync(
                appointment.DoctorId, appointment.RoomId, newStartTime, newEndTime, appointmentId);

            if (!isAvailable)
            {
                throw new InvalidOperationException("The requested time slot is not available");
            }

            // Aggiorna l'appuntamento
            appointment.StartTime = newStartTime;
            appointment.EndTime = newEndTime;
            appointment.Status = AppointmentStatus.Rescheduled.ToString();

            _unitOfWork.Repository<Appointment>().Update(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Rescheduled appointment {AppointmentId} to {NewStartTime}",
                appointmentId, newStartTime);

            return appointment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    public async Task<bool> ConfirmAppointmentAsync(Guid appointmentId)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            appointment.Status = AppointmentStatus.Confirmed.ToString();
            _unitOfWork.Repository<Appointment>().Update(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Confirmed appointment {AppointmentId}", appointmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    public async Task<bool> CompleteAppointmentAsync(Guid appointmentId, string? notes = null)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            appointment.Status = AppointmentStatus.Completed.ToString();
            if (!string.IsNullOrEmpty(notes))
            {
                appointment.Notes = $"{appointment.Notes ?? ""}\nCompletion notes: {notes}".Trim();
            }

            _unitOfWork.Repository<Appointment>().Update(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Completed appointment {AppointmentId}", appointmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    public async Task<bool> MarkNoShowAsync(Guid appointmentId)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            appointment.Status = AppointmentStatus.NoShow.ToString();
            _unitOfWork.Repository<Appointment>().Update(appointment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Marked appointment {AppointmentId} as no-show", appointmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking no-show for appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    // STATUS AND VALIDATION

    public async Task<bool> ValidateAppointmentAsync(Appointment appointment)
    {
        var errors = await GetValidationErrorsAsync(appointment);
        return !errors.Any();
    }

    public async Task<IEnumerable<string>> GetValidationErrorsAsync(Appointment appointment)
    {
        var errors = new List<string>();

        try
        {
            // 1. Validazione base
            if (appointment.StartTime >= appointment.EndTime)
            {
                errors.Add("Start time must be before end time");
            }

            if (appointment.StartTime <= DateTime.UtcNow)
            {
                errors.Add("Appointment cannot be scheduled in the past");
            }

            // 2. Validazione esistenza entità
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(appointment.PatientId);
            if (patient == null)
            {
                errors.Add("Patient not found");
            }

            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(appointment.DoctorId);
            if (doctor == null)
            {
                errors.Add("Doctor not found");
            }

            var room = await _unitOfWork.Repository<Room>().GetByIdAsync(appointment.RoomId);
            if (room == null)
            {
                errors.Add("Room not found");
            }

            // 3. Validazione disponibilità
            if (doctor != null && room != null)
            {
                var isAvailable = await _schedulingService.IsSlotAvailableAsync(
                    appointment.DoctorId, appointment.RoomId,
                    appointment.StartTime, appointment.EndTime, appointment.Id);

                if (!isAvailable)
                {
                    errors.Add("Time slot is not available");
                }
            }

            // 4. Validazione orari lavorativi (8:00 - 18:00)
            var hour = appointment.StartTime.Hour;
            if (hour < 8 || hour >= 18)
            {
                errors.Add("Appointment must be scheduled during working hours (8:00 - 18:00)");
            }

            // 5. Validazione durata minima/massima
            var duration = appointment.EndTime - appointment.StartTime;
            if (duration.TotalMinutes < 15)
            {
                errors.Add("Appointment duration must be at least 15 minutes");
            }
            if (duration.TotalHours > 4)
            {
                errors.Add("Appointment duration cannot exceed 4 hours");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating appointment");
            errors.Add("Validation error occurred");
        }

        return errors;
    }

    public async Task<bool> CanCancelAppointmentAsync(Guid appointmentId)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            // Non può cancellare se già completato, cancellato o no-show
            var currentStatus = appointment.Status;
            if (currentStatus == AppointmentStatus.Completed.ToString() ||
                currentStatus == AppointmentStatus.Cancelled.ToString() ||
                currentStatus == AppointmentStatus.NoShow.ToString())
            {
                return false;
            }

            // Non può cancellare se l'appuntamento è già iniziato
            if (appointment.StartTime <= DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if appointment can be cancelled");
            return false;
        }
    }

    public async Task<bool> CanRescheduleAppointmentAsync(Guid appointmentId)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            // Stesse regole della cancellazione
            return await CanCancelAppointmentAsync(appointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if appointment can be rescheduled");
            return false;
        }
    }

    // SEARCH AND FILTERING

    public async Task<IEnumerable<Appointment>> SearchAppointmentsAsync(Guid clinicId, AppointmentSearchCriteria criteria)
    {
        try
        {
            // Ottieni tutti gli appuntamenti della clinica
            var baseAppointments = await GetAppointmentsByClinicAsync(clinicId);
            var filteredAppointments = baseAppointments.AsQueryable();

            // Applica filtri
            if (criteria.StartDate.HasValue)
            {
                filteredAppointments = filteredAppointments.Where(a => a.StartTime >= criteria.StartDate.Value);
            }

            if (criteria.EndDate.HasValue)
            {
                filteredAppointments = filteredAppointments.Where(a => a.StartTime <= criteria.EndDate.Value);
            }

            if (criteria.DoctorId.HasValue)
            {
                filteredAppointments = filteredAppointments.Where(a => a.DoctorId == criteria.DoctorId.Value);
            }

            if (criteria.PatientId.HasValue)
            {
                filteredAppointments = filteredAppointments.Where(a => a.PatientId == criteria.PatientId.Value);
            }

            if (criteria.RoomId.HasValue)
            {
                filteredAppointments = filteredAppointments.Where(a => a.RoomId == criteria.RoomId.Value);
            }

            if (criteria.Status.HasValue)
            {
                filteredAppointments = filteredAppointments.Where(a => a.Status == criteria.Status.Value.ToString());
            }

            if (!string.IsNullOrEmpty(criteria.ServiceType))
            {
                filteredAppointments = filteredAppointments.Where(a =>
                    a.ServiceType != null && a.ServiceType.Contains(criteria.ServiceType));
            }

            if (!criteria.IncludeCancelled)
            {
                filteredAppointments = filteredAppointments.Where(a =>
                    a.Status != AppointmentStatus.Cancelled.ToString());
            }

            // Paginazione
            var result = filteredAppointments
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .OrderBy(a => a.StartTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching appointments for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(Guid clinicId, AppointmentStatus status, DateTime? date = null)
    {
        try
        {
            var appointments = await GetAppointmentsByClinicAsync(clinicId, date);
            return appointments.Where(a => a.Status == status.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointments by status for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetTodayAppointmentsAsync(Guid clinicId)
    {
        try
        {
            return await GetAppointmentsByClinicAsync(clinicId, DateTime.Today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's appointments for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid clinicId, int days = 7)
    {
        try
        {
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(days);

            var criteria = new AppointmentSearchCriteria
            {
                StartDate = startDate,
                EndDate = endDate,
                IncludeCancelled = false
            };

            return await SearchAppointmentsAsync(clinicId, criteria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming appointments for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    // ANALYTICS

    public async Task<AppointmentStatistics> GetAppointmentStatisticsAsync(Guid clinicId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var criteria = new AppointmentSearchCriteria
            {
                StartDate = startDate,
                EndDate = endDate,
                IncludeCancelled = true,
                PageSize = int.MaxValue // Ottieni tutti per le statistiche
            };

            var appointments = await SearchAppointmentsAsync(clinicId, criteria);
            var appointmentsList = appointments.ToList();

            var statistics = new AppointmentStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalAppointments = appointmentsList.Count
            };

            // Calcola statistiche per status
            var statusGroups = appointmentsList.GroupBy(a => a.Status);
            foreach (var group in statusGroups)
            {
                var count = group.Count();
                statistics.AppointmentsByStatus[group.Key] = count;

                switch (group.Key)
                {
                    case nameof(AppointmentStatus.Completed):
                        statistics.CompletedAppointments = count;
                        break;
                    case nameof(AppointmentStatus.Cancelled):
                        statistics.CancelledAppointments = count;
                        break;
                    case nameof(AppointmentStatus.NoShow):
                        statistics.NoShowAppointments = count;
                        break;
                    case nameof(AppointmentStatus.Scheduled):
                    case nameof(AppointmentStatus.Confirmed):
                        statistics.ScheduledAppointments += count;
                        break;
                }
            }

            // Calcola percentuali
            if (statistics.TotalAppointments > 0)
            {
                statistics.CompletionRate = (double)statistics.CompletedAppointments / statistics.TotalAppointments * 100;
                statistics.CancellationRate = (double)statistics.CancelledAppointments / statistics.TotalAppointments * 100;
                statistics.NoShowRate = (double)statistics.NoShowAppointments / statistics.TotalAppointments * 100;
            }

            // Statistiche per medico
            var doctorGroups = appointmentsList.GroupBy(a => a.DoctorId);
            foreach (var group in doctorGroups)
            {
                statistics.AppointmentsByDoctor[group.Key] = group.Count();
            }

            // Statistiche per sala
            var roomGroups = appointmentsList.GroupBy(a => a.RoomId);
            foreach (var group in roomGroups)
            {
                statistics.AppointmentsByRoom[group.Key] = group.Count();
            }

            // Statistiche temporali
            var hourGroups = appointmentsList.GroupBy(a => a.StartTime.Hour);
            foreach (var group in hourGroups)
            {
                statistics.AppointmentsByHour[group.Key] = group.Count();
            }

            var dayGroups = appointmentsList.GroupBy(a => a.StartTime.DayOfWeek);
            foreach (var group in dayGroups)
            {
                statistics.AppointmentsByDay[group.Key] = group.Count();
            }

            // Statistiche revenue
            var paidAppointments = appointmentsList.Where(a => a.Price.HasValue && a.IsPaid);
            var unpaidAppointments = appointmentsList.Where(a => a.Price.HasValue && !a.IsPaid);

            statistics.TotalRevenue = paidAppointments.Sum(a => a.Price!.Value);
            statistics.PendingRevenue = unpaidAppointments.Sum(a => a.Price!.Value);
            statistics.AverageAppointmentValue = appointmentsList.Where(a => a.Price.HasValue)
                .Average(a => a.Price!.Value);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment statistics for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<IEnumerable<Appointment>> GetConflictingAppointmentsAsync(Guid clinicId, DateTime date)
    {
        try
        {
            // Utilizza il Scheduling Service per rilevare conflitti
            var conflicts = await _schedulingService.DetectConflictsAsync(clinicId, date.Date, date.Date.AddDays(1));

            var conflictingAppointmentIds = conflicts
                .SelectMany(c => c.AffectedAppointmentIds)
                .Distinct();

            var conflictingAppointments = new List<Appointment>();
            foreach (var appointmentId in conflictingAppointmentIds)
            {
                var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(appointmentId);
                if (appointment != null)
                {
                    conflictingAppointments.Add(appointment);
                }
            }

            return conflictingAppointments.OrderBy(a => a.StartTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conflicting appointments for clinic {ClinicId}", clinicId);
            throw;
        }
    }
}