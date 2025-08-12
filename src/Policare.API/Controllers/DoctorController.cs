using Microsoft.AspNetCore.Mvc;
using PoliCare.Core.Entities;
using PoliCare.Services.Interfaces;
using Policare.API.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace Policare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(IDoctorService doctorService, ILogger<DoctorsController> logger)
    {
        _doctorService = doctorService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutti i medici di una clinica
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctorsByClinic(Guid clinicId)
    {
        try
        {
            var doctors = await _doctorService.GetDoctorsByClinicAsync(clinicId);
            var doctorDtos = doctors.Select(d => new DoctorDto
            {
                Id = d.Id,
                UserId = d.UserId,
                ClinicId = d.ClinicId,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                HourlyRate = d.HourlyRate,
                CommissionPercentage = d.CommissionPercentage,
                // Per ora lasciamo vuoti i campi User - li implementeremo dopo
                FirstName = "TBD", // TODO: Implementare caricamento User
                LastName = "TBD",
                Email = "TBD",
                Phone = "TBD",
                CreatedAt = d.CreatedAt
            });

            return Ok(doctorDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene un medico per ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DoctorDto>> GetDoctor(Guid id)
    {
        try
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            var doctorDto = new DoctorDto
            {
                Id = doctor.Id,
                UserId = doctor.UserId,
                ClinicId = doctor.ClinicId,
                Specialization = doctor.Specialization,
                LicenseNumber = doctor.LicenseNumber,
                HourlyRate = doctor.HourlyRate,
                CommissionPercentage = doctor.CommissionPercentage,
                // Per ora lasciamo vuoti i campi User
                FirstName = "TBD",
                LastName = "TBD",
                Email = "TBD",
                Phone = "TBD",
                CreatedAt = doctor.CreatedAt
            };

            return Ok(doctorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor {DoctorId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Crea un nuovo medico con utente associato
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DoctorDto>> CreateDoctor(CreateDoctorDto createDoctorDto)
    {
        try
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(createDoctorDto.Email))
            {
                return BadRequest("Email is required");
            }

            if (string.IsNullOrWhiteSpace(createDoctorDto.Password))
            {
                return BadRequest("Password is required");
            }

            // Crea User
            var user = new User
            {
                FirstName = createDoctorDto.FirstName,
                LastName = createDoctorDto.LastName,
                Email = createDoctorDto.Email,
                Phone = createDoctorDto.Phone,
                PasswordHash = HashPassword(createDoctorDto.Password),
                ClinicId = createDoctorDto.ClinicId,
                Role = UserRole.Doctor
            };

            // Crea Doctor
            var doctor = new Doctor
            {
                ClinicId = createDoctorDto.ClinicId,
                Specialization = createDoctorDto.Specialization,
                LicenseNumber = createDoctorDto.LicenseNumber,
                HourlyRate = createDoctorDto.HourlyRate,
                CommissionPercentage = createDoctorDto.CommissionPercentage,
                WorkingHours = "{}" // Default empty JSON
            };

            var createdDoctor = await _doctorService.CreateDoctorAsync(doctor, user);

            var doctorDto = new DoctorDto
            {
                Id = createdDoctor.Id,
                UserId = createdDoctor.UserId,
                ClinicId = createdDoctor.ClinicId,
                Specialization = createdDoctor.Specialization,
                LicenseNumber = createdDoctor.LicenseNumber,
                HourlyRate = createdDoctor.HourlyRate,
                CommissionPercentage = createdDoctor.CommissionPercentage,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                CreatedAt = createdDoctor.CreatedAt
            };

            return CreatedAtAction(nameof(GetDoctor), new { id = createdDoctor.Id }, doctorDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating doctor");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Aggiorna un medico esistente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDoctor(Guid id, UpdateDoctorDto updateDoctorDto)
    {
        try
        {
            var updatedDoctor = new Doctor
            {
                Specialization = updateDoctorDto.Specialization,
                LicenseNumber = updateDoctorDto.LicenseNumber,
                HourlyRate = updateDoctorDto.HourlyRate,
                CommissionPercentage = updateDoctorDto.CommissionPercentage
            };

            var result = await _doctorService.UpdateDoctorAsync(id, updatedDoctor);
            if (result == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating doctor {DoctorId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor {DoctorId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Elimina un medico (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        try
        {
            var result = await _doctorService.DeleteDoctorAsync(id);
            if (!result)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting doctor {DoctorId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cerca medici per specializzazione o numero licenza
    /// </summary>
    [HttpGet("clinic/{clinicId}/search")]
    public async Task<ActionResult<IEnumerable<DoctorDto>>> SearchDoctors(Guid clinicId, [FromQuery] string searchTerm)
    {
        try
        {
            var doctors = await _doctorService.SearchDoctorsAsync(clinicId, searchTerm);
            var doctorDtos = doctors.Select(d => new DoctorDto
            {
                Id = d.Id,
                UserId = d.UserId,
                ClinicId = d.ClinicId,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                HourlyRate = d.HourlyRate,
                CommissionPercentage = d.CommissionPercentage,
                FirstName = "TBD",
                LastName = "TBD",
                Email = "TBD",
                Phone = "TBD",
                CreatedAt = d.CreatedAt
            });

            return Ok(doctorDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching doctors for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Verifica disponibilità di un medico
    /// </summary>
    [HttpGet("{id}/availability")]
    public async Task<ActionResult<DoctorAvailabilityDto>> CheckAvailability(
        Guid id,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        try
        {
            var isAvailable = await _doctorService.IsAvailableAsync(id, startTime, endTime);

            var availabilityDto = new DoctorAvailabilityDto
            {
                DoctorId = id,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = isAvailable,
                ConflictReason = isAvailable ? null : "Doctor has conflicting appointments"
            };

            return Ok(availabilityDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for doctor {DoctorId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hash password using SHA256 (temporary - use BCrypt in production)
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}