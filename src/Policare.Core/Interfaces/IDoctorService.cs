using PoliCare.Core.Entities;

namespace PoliCare.Services.Interfaces;

public interface IDoctorService
{
    Task<IEnumerable<Doctor>> GetDoctorsByClinicAsync(Guid clinicId);
    Task<Doctor?> GetDoctorByIdAsync(Guid doctorId);
    Task<Doctor?> GetDoctorByUserIdAsync(Guid userId);
    Task<Doctor> CreateDoctorAsync(Doctor doctor, User user);
    Task<Doctor?> UpdateDoctorAsync(Guid doctorId, Doctor doctor);
    Task<bool> DeleteDoctorAsync(Guid doctorId);
    Task<IEnumerable<Doctor>> SearchDoctorsAsync(Guid clinicId, string searchTerm);
    Task<bool> DoctorExistsAsync(Guid clinicId, string licenseNumber);
    Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(Guid clinicId, string specialization);
    Task<bool> IsAvailableAsync(Guid doctorId, DateTime startTime, DateTime endTime);
}