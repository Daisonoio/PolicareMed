using Microsoft.AspNetCore.Mvc;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using Policare.API.DTOs;

namespace Policare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClinicsController> _logger;

    public ClinicsController(IUnitOfWork unitOfWork, ILogger<ClinicsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutte le cliniche
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicDto>>> GetClinics()
    {
        try
        {
            var clinics = await _unitOfWork.Repository<Clinic>().GetAllAsync();
            var clinicDtos = clinics.Select(c => new ClinicDto
            {
                Id = c.Id,
                Name = c.Name,
                Address = c.Address,
                Phone = c.Phone,
                Email = c.Email,
                VatNumber = c.VatNumber,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            });

            return Ok(clinicDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clinics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene una clinica per ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ClinicDto>> GetClinic(Guid id)
    {
        try
        {
            var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(id);
            if (clinic == null)
            {
                return NotFound($"Clinic with ID {id} not found");
            }

            var clinicDto = new ClinicDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                Email = clinic.Email,
                VatNumber = clinic.VatNumber,
                IsActive = clinic.IsActive,
                CreatedAt = clinic.CreatedAt
            };

            return Ok(clinicDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clinic with ID {ClinicId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Crea una nuova clinica
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClinicDto>> CreateClinic(CreateClinicDto createClinicDto)
    {
        try
        {
            // Verifica se esiste già una clinica con la stessa P.IVA
            if (!string.IsNullOrEmpty(createClinicDto.VatNumber))
            {
                var existingClinic = await _unitOfWork.Repository<Clinic>()
                    .GetFirstOrDefaultAsync(c => c.VatNumber == createClinicDto.VatNumber);

                if (existingClinic != null)
                {
                    return BadRequest("A clinic with this VAT number already exists");
                }
            }

            var clinic = new Clinic
            {
                Name = createClinicDto.Name,
                Address = createClinicDto.Address ?? string.Empty,
                Phone = createClinicDto.Phone ?? string.Empty,
                Email = createClinicDto.Email ?? string.Empty,
                VatNumber = createClinicDto.VatNumber ?? string.Empty,
                IsActive = true,
                Settings = "{}" // Inizializzazione corretta
            };

            await _unitOfWork.Repository<Clinic>().AddAsync(clinic);
            await _unitOfWork.CompleteAsync();

            var clinicDto = new ClinicDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                Email = clinic.Email,
                VatNumber = clinic.VatNumber,
                IsActive = clinic.IsActive,
                CreatedAt = clinic.CreatedAt
            };

            return CreatedAtAction(nameof(GetClinic), new { id = clinic.Id }, clinicDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clinic");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Aggiorna una clinica esistente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClinic(Guid id, UpdateClinicDto updateClinicDto)
    {
        try
        {
            var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(id);
            if (clinic == null)
            {
                return NotFound($"Clinic with ID {id} not found");
            }

            // Verifica P.IVA duplicata (esclusa la clinica corrente)
            if (!string.IsNullOrEmpty(updateClinicDto.VatNumber) &&
                updateClinicDto.VatNumber != clinic.VatNumber)
            {
                var existingClinic = await _unitOfWork.Repository<Clinic>()
                    .GetFirstOrDefaultAsync(c => c.VatNumber == updateClinicDto.VatNumber && c.Id != id);

                if (existingClinic != null)
                {
                    return BadRequest("A clinic with this VAT number already exists");
                }
            }

            clinic.Name = updateClinicDto.Name;
            clinic.Address = updateClinicDto.Address ?? string.Empty;
            clinic.Phone = updateClinicDto.Phone ?? string.Empty;
            clinic.Email = updateClinicDto.Email ?? string.Empty;
            clinic.VatNumber = updateClinicDto.VatNumber ?? string.Empty;

            _unitOfWork.Repository<Clinic>().Update(clinic);
            await _unitOfWork.CompleteAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating clinic with ID {ClinicId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Disattiva una clinica (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClinic(Guid id)
    {
        try
        {
            var result = await _unitOfWork.Repository<Clinic>().DeleteAsync(id);
            if (!result)
            {
                return NotFound($"Clinic with ID {id} not found");
            }

            await _unitOfWork.CompleteAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting clinic with ID {ClinicId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}