using Microsoft.Extensions.Logging;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;

namespace PoliCare.Services.Services;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomService> _logger;

    public RoomService(IUnitOfWork unitOfWork, ILogger<RoomService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>> GetRoomsByClinicAsync(Guid clinicId)
    {
        try
        {
            var rooms = await _unitOfWork.Repository<Room>()
                .GetWhereAsync(r => r.ClinicId == clinicId);

            return rooms.OrderBy(r => r.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsByClinicAsync(Guid clinicId)
    {
        try
        {
            var rooms = await _unitOfWork.Repository<Room>()
                .GetWhereAsync(r => r.ClinicId == clinicId && r.IsActive);

            return rooms.OrderBy(r => r.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active rooms for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<Room?> GetRoomByIdAsync(Guid roomId)
    {
        try
        {
            return await _unitOfWork.Repository<Room>().GetByIdAsync(roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {RoomId}", roomId);
            throw;
        }
    }

    public async Task<Room?> GetRoomByCodeAsync(Guid clinicId, string code)
    {
        try
        {
            return await _unitOfWork.Repository<Room>()
                .GetFirstOrDefaultAsync(r => r.ClinicId == clinicId && r.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room by code {Code} for clinic {ClinicId}", code, clinicId);
            throw;
        }
    }

    public async Task<Room> CreateRoomAsync(Room room)
    {
        try
        {
            // Verifica se esiste già una sala con lo stesso codice nella clinica
            var existingRoom = await _unitOfWork.Repository<Room>()
                .GetFirstOrDefaultAsync(r => r.ClinicId == room.ClinicId && r.Code == room.Code);

            if (existingRoom != null)
            {
                throw new InvalidOperationException($"A room with code {room.Code} already exists in this clinic");
            }

            await _unitOfWork.Repository<Room>().AddAsync(room);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Created room {RoomId} with code {Code} for clinic {ClinicId}",
                room.Id, room.Code, room.ClinicId);
            return room;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room for clinic {ClinicId}", room.ClinicId);
            throw;
        }
    }

    public async Task<Room?> UpdateRoomAsync(Guid roomId, Room updatedRoom)
    {
        try
        {
            var existingRoom = await _unitOfWork.Repository<Room>().GetByIdAsync(roomId);
            if (existingRoom == null)
            {
                return null;
            }

            // Verifica codice duplicato (esclusa sala corrente)
            if (updatedRoom.Code != existingRoom.Code)
            {
                var duplicateRoom = await _unitOfWork.Repository<Room>()
                    .GetFirstOrDefaultAsync(r => r.ClinicId == existingRoom.ClinicId &&
                                                r.Code == updatedRoom.Code &&
                                                r.Id != roomId);

                if (duplicateRoom != null)
                {
                    throw new InvalidOperationException($"A room with code {updatedRoom.Code} already exists in this clinic");
                }
            }

            // Aggiorna proprietà
            existingRoom.Name = updatedRoom.Name;
            existingRoom.Description = updatedRoom.Description;
            existingRoom.Code = updatedRoom.Code;
            existingRoom.IsActive = updatedRoom.IsActive;

            _unitOfWork.Repository<Room>().Update(existingRoom);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Updated room {RoomId}", roomId);
            return existingRoom;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", roomId);
            throw;
        }
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId)
    {
        try
        {
            // Verifica se la sala ha appuntamenti attivi
            var hasActiveAppointments = await _unitOfWork.Repository<Appointment>()
                .ExistsAsync(a => a.RoomId == roomId &&
                               a.StartTime > DateTime.UtcNow &&
                               a.Status != AppointmentStatus.Cancelled.ToString());

            if (hasActiveAppointments)
            {
                throw new InvalidOperationException("Cannot delete room with active appointments");
            }

            var result = await _unitOfWork.Repository<Room>().DeleteAsync(roomId);
            if (result)
            {
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Deleted room {RoomId}", roomId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", roomId);
            throw;
        }
    }

    public async Task<IEnumerable<Room>> SearchRoomsAsync(Guid clinicId, string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetRoomsByClinicAsync(clinicId);
            }

            var lowerSearchTerm = searchTerm.ToLower();

            var rooms = await _unitOfWork.Repository<Room>()
                .GetWhereAsync(r => r.ClinicId == clinicId &&
                              (r.Name.ToLower().Contains(lowerSearchTerm) ||
                               r.Code.ToLower().Contains(lowerSearchTerm) ||
                               (r.Description != null && r.Description.ToLower().Contains(lowerSearchTerm))));

            return rooms.OrderBy(r => r.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching rooms for clinic {ClinicId} with term {SearchTerm}", clinicId, searchTerm);
            throw;
        }
    }

    public async Task<bool> RoomExistsAsync(Guid clinicId, string code)
    {
        try
        {
            return await _unitOfWork.Repository<Room>()
                .ExistsAsync(r => r.ClinicId == clinicId && r.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking room existence for clinic {ClinicId} and code {Code}", clinicId, code);
            throw;
        }
    }

    public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime)
    {
        try
        {
            // Verifica conflitti con appuntamenti esistenti
            var hasConflict = await _unitOfWork.Repository<Appointment>()
                .ExistsAsync(a => a.RoomId == roomId &&
                                 a.Status != AppointmentStatus.Cancelled.ToString() &&
                                 a.Status != AppointmentStatus.NoShow.ToString() &&
                                 ((a.StartTime < endTime && a.EndTime > startTime)));

            return !hasConflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking room {RoomId} availability", roomId);
            throw;
        }
    }
}