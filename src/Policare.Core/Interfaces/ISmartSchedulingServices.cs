using PoliCare.Core.Entities;

namespace PoliCare.Core.Interfaces;

public interface ISmartSchedulingService
{
    Task<IEnumerable<TimeSlot>> GetAvailableSlotsAsync(
        Guid doctorId,
        DateTime startDate,
        DateTime endDate,
        int durationMinutes);

    Task<Appointment?> FindOptimalSlotAsync(
        Guid patientId,
        Guid doctorId,
        DateTime preferredDate,
        int durationMinutes,
        AppointmentType type);

    Task<bool> OptimizeScheduleAsync(
        Guid clinicId,
        DateTime date);

    Task<IEnumerable<Appointment>> GetConflictsAsync(
        Guid clinicId,
        DateTime startDate,
        DateTime endDate);
}