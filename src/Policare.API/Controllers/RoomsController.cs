using Microsoft.AspNetCore.Mvc;
using PoliCare.Core.Entities;
using PoliCare.Services.Interfaces;
using Policare.API.DTOs;

namespace Policare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(IRoomService roomService, ILogger<RoomsController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutte le sale di una clinica
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRoomsByClinic(Guid clinicId)
    {
        try
        {
            var rooms = await _roomService.GetRoomsByClinicAsync(clinicId);
            var roomDtos = rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                ClinicId = r.ClinicId,
                Name = r.Name,
                Description = r.Description,
                Code = r.Code,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            });

            return Ok(roomDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene solo le sale attive di una clinica
    /// </summary>
    [HttpGet("clinic/{clinicId}/active")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetActiveRoomsByClinic(Guid clinicId)
    {
        try
        {
            var rooms = await _roomService.GetActiveRoomsByClinicAsync(clinicId);
            var roomDtos = rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                ClinicId = r.ClinicId,
                Name = r.Name,
                Description = r.Description,
                Code = r.Code,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            });

            return Ok(roomDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active rooms for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene una sala per ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetRoom(Guid id)
    {
        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound($"Room with ID {id} not found");
            }

            var roomDto = new RoomDto
            {
                Id = room.Id,
                ClinicId = room.ClinicId,
                Name = room.Name,
                Description = room.Description,
                Code = room.Code,
                IsActive = room.IsActive,
                CreatedAt = room.CreatedAt
            };

            return Ok(roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ottiene una sala per codice
    /// </summary>
    [HttpGet("clinic/{clinicId}/code/{code}")]
    public async Task<ActionResult<RoomDto>> GetRoomByCode(Guid clinicId, string code)
    {
        try
        {
            var room = await _roomService.GetRoomByCodeAsync(clinicId, code);
            if (room == null)
            {
                return NotFound($"Room with code {code} not found in clinic {clinicId}");
            }

            var roomDto = new RoomDto
            {
                Id = room.Id,
                ClinicId = room.ClinicId,
                Name = room.Name,
                Description = room.Description,
                Code = room.Code,
                IsActive = room.IsActive,
                CreatedAt = room.CreatedAt
            };

            return Ok(roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room by code {Code} for clinic {ClinicId}", code, clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Crea una nuova sala
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomDto createRoomDto)
    {
        try
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(createRoomDto.Name))
            {
                return BadRequest("Room name is required");
            }

            if (string.IsNullOrWhiteSpace(createRoomDto.Code))
            {
                return BadRequest("Room code is required");
            }

            var room = new Room
            {
                ClinicId = createRoomDto.ClinicId,
                Name = createRoomDto.Name,
                Description = createRoomDto.Description,
                Code = createRoomDto.Code,
                IsActive = createRoomDto.IsActive
            };

            var createdRoom = await _roomService.CreateRoomAsync(room);

            var roomDto = new RoomDto
            {
                Id = createdRoom.Id,
                ClinicId = createdRoom.ClinicId,
                Name = createdRoom.Name,
                Description = createdRoom.Description,
                Code = createdRoom.Code,
                IsActive = createdRoom.IsActive,
                CreatedAt = createdRoom.CreatedAt
            };

            return CreatedAtAction(nameof(GetRoom), new { id = createdRoom.Id }, roomDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating room");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Aggiorna una sala esistente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoom(Guid id, UpdateRoomDto updateRoomDto)
    {
        try
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(updateRoomDto.Name))
            {
                return BadRequest("Room name is required");
            }

            if (string.IsNullOrWhiteSpace(updateRoomDto.Code))
            {
                return BadRequest("Room code is required");
            }

            var updatedRoom = new Room
            {
                Name = updateRoomDto.Name,
                Description = updateRoomDto.Description,
                Code = updateRoomDto.Code,
                IsActive = updateRoomDto.IsActive
            };

            var result = await _roomService.UpdateRoomAsync(id, updatedRoom);
            if (result == null)
            {
                return NotFound($"Room with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating room {RoomId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Elimina una sala (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        try
        {
            var result = await _roomService.DeleteRoomAsync(id);
            if (!result)
            {
                return NotFound($"Room with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete room {RoomId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cerca sale per nome, codice o descrizione
    /// </summary>
    [HttpGet("clinic/{clinicId}/search")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> SearchRooms(Guid clinicId, [FromQuery] string searchTerm)
    {
        try
        {
            var rooms = await _roomService.SearchRoomsAsync(clinicId, searchTerm);
            var roomDtos = rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                ClinicId = r.ClinicId,
                Name = r.Name,
                Description = r.Description,
                Code = r.Code,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            });

            return Ok(roomDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching rooms for clinic {ClinicId}", clinicId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Verifica disponibilità di una sala
    /// </summary>
    [HttpGet("{id}/availability")]
    public async Task<ActionResult<RoomAvailabilityDto>> CheckAvailability(
        Guid id,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        try
        {
            var isAvailable = await _roomService.IsRoomAvailableAsync(id, startTime, endTime);

            var availabilityDto = new RoomAvailabilityDto
            {
                RoomId = id,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = isAvailable,
                ConflictReason = isAvailable ? null : "Room has conflicting appointments"
            };

            return Ok(availabilityDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for room {RoomId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Verifica se esiste una sala con il codice specificato
    /// </summary>
    [HttpGet("clinic/{clinicId}/exists/{code}")]
    public async Task<ActionResult<bool>> RoomExists(Guid clinicId, string code)
    {
        try
        {
            var exists = await _roomService.RoomExistsAsync(clinicId, code);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking room existence for clinic {ClinicId} and code {Code}", clinicId, code);
            return StatusCode(500, "Internal server error");
        }
    }
}