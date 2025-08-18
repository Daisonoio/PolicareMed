using Microsoft.AspNetCore.Mvc;
using PoliCare.Core.Entities;
using PoliCare.Services.Interfaces;
using Policare.API.DTOs;

namespace Policare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        ISchedulingService schedulingService,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutti gli appuntamenti di una clinica
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByClinic(
        Guid clinicId,
        [FromQuery] DateTime? date = null)
    {
        try
        {
            var appointments = await _appointmentService.GetAppointmentsByClinicAsync(clinicId, date);
            var appointmentDtos = await MapToAppointmentDtosAsync(appointments);

            return Ok(appointmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene appuntamenti per medico
    /// </summary>
    [HttpGet("doctor/{doctorId}")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByDoctor(
        Guid doctorId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var appointments = await _appointmentService.GetAppointmentsByDoctorAsync(doctorId, startDate, endDate);
            var appointmentDtos = await MapToAppointmentDtosAsync(appointments);

            return Ok(appointmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene appuntamenti per paziente
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByPatient(Guid patientId)
    {
        try
        {
            var appointments = await _appointmentService.GetAppointmentsByPatientAsync(patientId);
            var appointmentDtos = await MapToAppointmentDtosAsync(appointments);

            return Ok(appointmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene un appuntamento specifico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(Guid id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            var appointmentDto = await MapToAppointmentDtoAsync(appointment);
            return Ok(appointmentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Crea un appuntamento standard
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentDto createDto)
    {
        try
        {
            var appointment = new Appointment
            {
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                RoomId = createDto.RoomId,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                ServiceType = createDto.ServiceType,
                Notes = createDto.Notes,
                Price = createDto.Price,
                IsPaid = createDto.IsPaid,
                Status = AppointmentStatus.Scheduled.ToString()
            };

            var createdAppointment = await _appointmentService.CreateAppointmentAsync(appointment);
            var appointmentDto = await MapToAppointmentDtoAsync(createdAppointment);

            return CreatedAtAction(nameof(GetAppointment), new { id = createdAppointment.Id }, appointmentDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating appointment");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 🧠 SMART SCHEDULING: Crea un appuntamento utilizzando l'algoritmo intelligente
    /// </summary>
    [HttpPost("smart")]
    public async Task<ActionResult<AppointmentDto>> CreateSmartAppointment(CreateSmartAppointmentDto createDto)
    {
        try
        {
            // Converte preferences DTO a domain model
            SchedulingPreferences? preferences = null;
            if (createDto.Preferences != null)
            {
                preferences = new SchedulingPreferences
                {
                    PreferredStartTime = createDto.Preferences.PreferredStartTime,
                    PreferredEndTime = createDto.Preferences.PreferredEndTime,
                    PreferredDays = createDto.Preferences.PreferredDays,
                    ExcludedDays = createDto.Preferences.ExcludedDays,
                    MaxDaysFromPreferred = createDto.Preferences.MaxDaysFromPreferred,
                    PreferMorning = createDto.Preferences.PreferMorning,
                    PreferAfternoon = createDto.Preferences.PreferAfternoon,
                    PreferredRoomId = createDto.Preferences.PreferredRoomId,
                    Priority = createDto.Preferences.Priority
                };
            }

            var request = new CreateAppointmentRequest
            {
                ClinicId = createDto.ClinicId,
                PatientId = createDto.PatientId,
                PreferredDoctorId = createDto.PreferredDoctorId,
                RequiredSpecialization = createDto.RequiredSpecialization,
                PreferredDate = createDto.PreferredDate,
                DurationMinutes = createDto.DurationMinutes,
                Type = createDto.Type,
                ServiceType = createDto.ServiceType,
                Notes = createDto.Notes,
                Price = createDto.Price,
                Preferences = preferences,
                AutoOptimize = createDto.AutoOptimize
            };

            var createdAppointment = await _appointmentService.CreateSmartAppointmentAsync(request);
            var appointmentDto = await MapToAppointmentDtoAsync(createdAppointment);

            return CreatedAtAction(nameof(GetAppointment), new { id = createdAppointment.Id }, appointmentDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Smart scheduling error creating appointment");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating smart appointment");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Aggiorna un appuntamento
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(Guid id, UpdateAppointmentDto updateDto)
    {
        try
        {
            var updatedAppointment = new Appointment
            {
                StartTime = updateDto.StartTime,
                EndTime = updateDto.EndTime,
                DoctorId = updateDto.DoctorId,
                RoomId = updateDto.RoomId,
                ServiceType = updateDto.ServiceType,
                Notes = updateDto.Notes,
                Price = updateDto.Price,
                IsPaid = updateDto.IsPaid
            };

            var result = await _appointmentService.UpdateAppointmentAsync(id, updatedAppointment);
            if (result == null)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating appointment {AppointmentId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancella un appuntamento
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromQuery] string? reason = null)
    {
        try
        {
            var result = await _appointmentService.CancelAppointmentAsync(id, reason ?? "Cancelled by user");
            if (!result)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot cancel appointment {AppointmentId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Riprogramma un appuntamento
    /// </summary>
    [HttpPost("{id}/reschedule")]
    public async Task<ActionResult<AppointmentDto>> RescheduleAppointment(Guid id, RescheduleAppointmentDto rescheduleDto)
    {
        try
        {
            Appointment? rescheduledAppointment;

            if (rescheduleDto.UseSmartScheduling)
            {
                // Usa Smart Scheduling per trovare il miglior slot
                var preferences = rescheduleDto.Preferences != null ? new SchedulingPreferences
                {
                    PreferredStartTime = rescheduleDto.Preferences.PreferredStartTime,
                    PreferredEndTime = rescheduleDto.Preferences.PreferredEndTime,
                    PreferredDays = rescheduleDto.Preferences.PreferredDays,
                    ExcludedDays = rescheduleDto.Preferences.ExcludedDays,
                    MaxDaysFromPreferred = rescheduleDto.Preferences.MaxDaysFromPreferred,
                    PreferMorning = rescheduleDto.Preferences.PreferMorning,
                    PreferAfternoon = rescheduleDto.Preferences.PreferAfternoon,
                    PreferredRoomId = rescheduleDto.Preferences.PreferredRoomId,
                    Priority = rescheduleDto.Preferences.Priority
                } : null;

                rescheduledAppointment = await _schedulingService.RescheduleAppointmentAsync(
                    id, rescheduleDto.NewStartTime, preferences);
            }
            else
            {
                // Riprogrammazione manuale
                rescheduledAppointment = await _appointmentService.RescheduleAppointmentAsync(
                    id, rescheduleDto.NewStartTime, rescheduleDto.NewEndTime);
            }

            if (rescheduledAppointment == null)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            var appointmentDto = await MapToAppointmentDtoAsync(rescheduledAppointment);
            return Ok(appointmentDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot reschedule appointment {AppointmentId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Conferma un appuntamento
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmAppointment(Guid id)
    {
        try
        {
            var result = await _appointmentService.ConfirmAppointmentAsync(id);
            if (!result)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            return Ok(new { message = "Appointment confirmed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Completa un appuntamento
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteAppointment(Guid id, [FromBody] string? notes = null)
    {
        try
        {
            var result = await _appointmentService.CompleteAppointmentAsync(id, notes);
            if (!result)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            return Ok(new { message = "Appointment completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Marca un appuntamento come no-show
    /// </summary>
    [HttpPost("{id}/no-show")]
    public async Task<IActionResult> MarkNoShow(Guid id)
    {
        try
        {
            var result = await _appointmentService.MarkNoShowAsync(id);
            if (!result)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            return Ok(new { message = "Appointment marked as no-show" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking no-show for appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cerca appuntamenti con criteri avanzati
    /// </summary>
    [HttpPost("clinic/{clinicId}/search")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> SearchAppointments(
        Guid clinicId,
        AppointmentSearchDto searchDto)
    {
        try
        {
            var criteria = new AppointmentSearchCriteria
            {
                StartDate = searchDto.StartDate,
                EndDate = searchDto.EndDate,
                DoctorId = searchDto.DoctorId,
                PatientId = searchDto.PatientId,
                RoomId = searchDto.RoomId,
                Status = searchDto.Status,
                ServiceType = searchDto.ServiceType,
                SearchTerm = searchDto.SearchTerm,
                IncludeCancelled = searchDto.IncludeCancelled,
                PageNumber = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            var appointments = await _appointmentService.SearchAppointmentsAsync(clinicId, criteria);
            var appointmentDtos = await MapToAppointmentDtosAsync(appointments);

            return Ok(appointmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching appointments for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene appuntamenti di oggi
    /// </summary>
    [HttpGet("clinic/{clinicId}/today")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetTodayAppointments(Guid clinicId)
    {
        try
        {
            var appointments = await _appointmentService.GetTodayAppointmentsAsync(clinicId);
            var appointmentDtos = await MapToAppointmentDtosAsync(appointments);

            return Ok(appointmentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's appointments for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene statistiche appuntamenti
    /// </summary>
    [HttpGet("clinic/{clinicId}/statistics")]
    public async Task<ActionResult<AppointmentStatisticsDto>> GetAppointmentStatistics(
        Guid clinicId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var statistics = await _appointmentService.GetAppointmentStatisticsAsync(clinicId, startDate, endDate);

            var statisticsDto = new AppointmentStatisticsDto
            {
                StartDate = statistics.StartDate,
                EndDate = statistics.EndDate,
                TotalAppointments = statistics.TotalAppointments,
                CompletedAppointments = statistics.CompletedAppointments,
                CancelledAppointments = statistics.CancelledAppointments,
                NoShowAppointments = statistics.NoShowAppointments,
                ScheduledAppointments = statistics.ScheduledAppointments,
                CompletionRate = statistics.CompletionRate,
                CancellationRate = statistics.CancellationRate,
                NoShowRate = statistics.NoShowRate,
                TotalRevenue = statistics.TotalRevenue,
                AverageAppointmentValue = statistics.AverageAppointmentValue,
                PendingRevenue = statistics.PendingRevenue,
                AppointmentsByStatus = statistics.AppointmentsByStatus,
                AppointmentsByDoctor = statistics.AppointmentsByDoctor,
                AppointmentsByRoom = statistics.AppointmentsByRoom,
                AppointmentsByHour = statistics.AppointmentsByHour,
                AppointmentsByDay = statistics.AppointmentsByDay
            };

            return Ok(statisticsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment statistics for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    // SMART SCHEDULING ENDPOINTS

    /// <summary>
    /// 🧠 TROVA SLOT OTTIMALE: Utilizza l'algoritmo intelligente
    /// </summary>
    [HttpPost("find-optimal-slot")]
    public async Task<ActionResult<OptimalSlotDto>> FindOptimalSlot([FromBody] FindOptimalSlotRequest request)
    {
        try
        {
            var preferences = request.Preferences != null ? new SchedulingPreferences
            {
                PreferredStartTime = request.Preferences.PreferredStartTime,
                PreferredEndTime = request.Preferences.PreferredEndTime,
                PreferredDays = request.Preferences.PreferredDays,
                ExcludedDays = request.Preferences.ExcludedDays,
                MaxDaysFromPreferred = request.Preferences.MaxDaysFromPreferred,
                PreferMorning = request.Preferences.PreferMorning,
                PreferAfternoon = request.Preferences.PreferAfternoon,
                PreferredRoomId = request.Preferences.PreferredRoomId,
                Priority = request.Preferences.Priority
            } : null;

            var optimalSlot = await _schedulingService.FindOptimalSlotAsync(
                request.ClinicId,
                request.PreferredDoctorId,
                request.PatientId,
                request.PreferredDate,
                request.DurationMinutes,
                request.AppointmentType,
                preferences);

            if (optimalSlot == null)
            {
                return NotFound("No optimal slot found for the specified criteria");
            }

            var slotDto = new OptimalSlotDto
            {
                StartTime = optimalSlot.StartTime,
                EndTime = optimalSlot.EndTime,
                DoctorId = optimalSlot.DoctorId,
                RoomId = optimalSlot.RoomId,
                IsAvailable = optimalSlot.IsAvailable,
                OptimalityScore = optimalSlot.OptimalityScore,
                UtilizationScore = optimalSlot.UtilizationScore,
                PreferenceScore = optimalSlot.PreferenceScore,
                DoctorName = optimalSlot.DoctorName,
                RoomName = optimalSlot.RoomName,
                Specialization = optimalSlot.Specialization,
                OptimizationFactors = optimalSlot.OptimizationFactors,
                ConflictReasons = optimalSlot.ConflictReasons
            };

            return Ok(slotDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding optimal slot");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 📅 OTTIENI SLOT DISPONIBILI: Con scoring di ottimalità
    /// </summary>
    [HttpGet("clinic/{clinicId}/available-slots")]
    public async Task<ActionResult<IEnumerable<OptimalSlotDto>>> GetAvailableSlots(
        Guid clinicId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int durationMinutes = 30,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] Guid? roomId = null)
    {
        try
        {
            var slots = await _schedulingService.GetAvailableSlotsAsync(
                clinicId, startDate, endDate, durationMinutes, doctorId, roomId);

            var slotDtos = slots.Select(slot => new OptimalSlotDto
            {
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                DoctorId = slot.DoctorId,
                RoomId = slot.RoomId,
                IsAvailable = slot.IsAvailable,
                OptimalityScore = slot.OptimalityScore,
                UtilizationScore = slot.UtilizationScore,
                PreferenceScore = slot.PreferenceScore,
                DoctorName = slot.DoctorName,
                RoomName = slot.RoomName,
                Specialization = slot.Specialization,
                OptimizationFactors = slot.OptimizationFactors,
                ConflictReasons = slot.ConflictReasons
            });

            return Ok(slotDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available slots for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// ⚡ OTTIMIZZA AGENDA: Algoritmo di ottimizzazione automatica
    /// </summary>
    [HttpPost("clinic/{clinicId}/optimize")]
    public async Task<ActionResult<ScheduleOptimizationResultDto>> OptimizeSchedule(
        Guid clinicId,
        [FromQuery] DateTime date,
        [FromQuery] OptimizationStrategy strategy = OptimizationStrategy.Balanced)
    {
        try
        {
            var result = await _schedulingService.OptimizeScheduleAsync(clinicId, date, strategy);

            var resultDto = new ScheduleOptimizationResultDto
            {
                Success = result.Success,
                AppointmentsOptimized = result.AppointmentsOptimized,
                ConflictsResolved = result.ConflictsResolved,
                UtilizationImprovement = result.UtilizationImprovement,
                TimeSaved = result.TimeSaved,
                Changes = result.Changes,
                Warnings = result.Warnings,
                OptimizationDate = result.OptimizationDate,
                StrategyUsed = result.StrategyUsed
            };

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing schedule for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 🔍 RILEVA CONFLITTI: Analisi conflitti di scheduling
    /// </summary>
    [HttpGet("clinic/{clinicId}/conflicts")]
    public async Task<ActionResult<IEnumerable<SchedulingConflictDto>>> DetectConflicts(
        Guid clinicId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var conflicts = await _schedulingService.DetectConflictsAsync(clinicId, startDate, endDate);

            var conflictDtos = conflicts.Select(conflict => new SchedulingConflictDto
            {
                Id = conflict.Id,
                Type = conflict.Type,
                Severity = conflict.Severity,
                Description = conflict.Description,
                AffectedAppointmentIds = conflict.AffectedAppointmentIds,
                ConflictTime = conflict.ConflictTime,
                SuggestedResolutions = conflict.SuggestedResolutions,
                CanAutoResolve = conflict.CanAutoResolve
            });

            return Ok(conflictDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting conflicts for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    // HELPER METHODS

    private async Task<IEnumerable<AppointmentDto>> MapToAppointmentDtosAsync(IEnumerable<Appointment> appointments)
    {
        var dtos = new List<AppointmentDto>();
        foreach (var appointment in appointments)
        {
            var dto = await MapToAppointmentDtoAsync(appointment);
            dtos.Add(dto);
        }
        return dtos;
    }

    private async Task<AppointmentDto> MapToAppointmentDtoAsync(Appointment appointment)
    {
        // Per ora mapping semplificato - in futuro si possono aggiungere i nomi
        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            RoomId = appointment.RoomId,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            ServiceType = appointment.ServiceType,
            Notes = appointment.Notes,
            Price = appointment.Price,
            IsPaid = appointment.IsPaid,
            PatientName = "TBD", // TODO: Caricare nomi dalle relazioni
            DoctorName = "TBD",
            RoomName = "TBD",
            Specialization = "TBD",
            CreatedAt = appointment.CreatedAt
        };
    }
}

/// <summary>
/// Request DTO per ricerca slot ottimale
/// </summary>
public class FindOptimalSlotRequest
{
    public Guid ClinicId { get; set; }
    public Guid? PreferredDoctorId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime PreferredDate { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentType AppointmentType { get; set; } = AppointmentType.FirstVisit;
    public SmartSchedulingPreferencesDto? Preferences { get; set; }
}