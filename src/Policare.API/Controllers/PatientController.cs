using Microsoft.AspNetCore.Mvc;
using Policare.API.DTO;
using Policare.API.DTOs;
using PoliCare.Core.Entities;
using PoliCare.Services.Interfaces;

namespace Policare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService patientService, ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutti i pazienti di una clinica
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetPatientsByClinic(Guid clinicId)
    {
        try
        {
            var patients = await _patientService.GetPatientsByClinicAsync(clinicId);
            var patientDtos = patients.Select(p => new PatientDto
            {
                Id = p.Id,
                ClinicId = p.ClinicId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                DateOfBirth = p.DateOfBirth,
                FiscalCode = p.FiscalCode,
                Address = p.Address,
                CreatedAt = p.CreatedAt
            });

            return Ok(patientDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene un paziente per ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PatientDto>> GetPatient(Guid id)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            var patientDto = new PatientDto
            {
                Id = patient.Id,
                ClinicId = patient.ClinicId,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                DateOfBirth = patient.DateOfBirth,
                FiscalCode = patient.FiscalCode,
                Address = patient.Address,
                CreatedAt = patient.CreatedAt
            };

            return Ok(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient {PatientId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Crea un nuovo paziente
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PatientDto>> CreatePatient(CreatePatientDto createPatientDto)
    {
        try
        {
            var patient = new Patient
            {
                ClinicId = createPatientDto.ClinicId,
                FirstName = createPatientDto.FirstName,
                LastName = createPatientDto.LastName,
                Email = createPatientDto.Email,
                Phone = createPatientDto.Phone,
                DateOfBirth = createPatientDto.DateOfBirth,
                FiscalCode = createPatientDto.FiscalCode,
                Address = createPatientDto.Address,
                MedicalHistory = "{}",
                Preferences = "{}"
            };

            var createdPatient = await _patientService.CreatePatientAsync(patient);

            var patientDto = new PatientDto
            {
                Id = createdPatient.Id,
                ClinicId = createdPatient.ClinicId,
                FirstName = createdPatient.FirstName,
                LastName = createdPatient.LastName,
                Email = createdPatient.Email,
                Phone = createdPatient.Phone,
                DateOfBirth = createdPatient.DateOfBirth,
                FiscalCode = createdPatient.FiscalCode,
                Address = createdPatient.Address,
                CreatedAt = createdPatient.CreatedAt
            };

            return CreatedAtAction(nameof(GetPatient), new { id = createdPatient.Id }, patientDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating patient");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Aggiorna un paziente esistente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatient(Guid id, UpdatePatientDto updatePatientDto)
    {
        try
        {
            var updatedPatient = new Patient
            {
                FirstName = updatePatientDto.FirstName,
                LastName = updatePatientDto.LastName,
                Email = updatePatientDto.Email,
                Phone = updatePatientDto.Phone,
                DateOfBirth = updatePatientDto.DateOfBirth,
                FiscalCode = updatePatientDto.FiscalCode,
                Address = updatePatientDto.Address
            };

            var result = await _patientService.UpdatePatientAsync(id, updatedPatient);
            if (result == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating patient {PatientId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient {PatientId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Elimina un paziente (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        try
        {
            var result = await _patientService.DeletePatientAsync(id);
            if (!result)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient {PatientId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cerca pazienti per nome, cognome, codice fiscale o email
    /// </summary>
    [HttpGet("clinic/{clinicId}/search")]
    public async Task<ActionResult<IEnumerable<PatientDto>>> SearchPatients(Guid clinicId, [FromQuery] string searchTerm)
    {
        try
        {
            var patients = await _patientService.SearchPatientsAsync(clinicId, searchTerm);
            var patientDtos = patients.Select(p => new PatientDto
            {
                Id = p.Id,
                ClinicId = p.ClinicId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                DateOfBirth = p.DateOfBirth,
                FiscalCode = p.FiscalCode,
                Address = p.Address,
                CreatedAt = p.CreatedAt
            });

            return Ok(patientDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching patients for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }
}