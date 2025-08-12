using PoliCare.Core.Entities;

namespace PoliCare.Services.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetRoomsByClinicAsync(Guid clinicId);
    Task<Room?> GetRoomByIdAsync(Guid roomId);
    Task<Room> CreateRoomAsync(Room room);
    Task<Room?> UpdateRoomAsync(Guid roomId, Room room);
    Task<bool> DeleteRoomAsync(Guid roomId);
    Task<IEnumerable<Room>> SearchRoomsAsync(Guid clinicId, string searchTerm);
    Task<bool> RoomExistsAsync(Guid clinicId, string code);
    Task<IEnumerable<Room>> GetActiveRoomsByClinicAsync(Guid clinicId);
    Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime);
    Task<Room?> GetRoomByCodeAsync(Guid clinicId, string code);
}