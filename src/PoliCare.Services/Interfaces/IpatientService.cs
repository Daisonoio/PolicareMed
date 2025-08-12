// src/PoliCare.Services/Interfaces/IPatientService.cs
using PoliCare.Core.Entities;

namespace PoliCare.Services.Interfaces;

public interface IPatientService
{
    Task<IEnumerable<Patient>> GetPatientsByClinicAsync(Guid clinicId);
    Task<Patient?> GetPatientByIdAsync(Guid patientId);
    Task<Patient> CreatePatientAsync(Patient patient);
    Task<Patient?> UpdatePatientAsync(Guid patientId, Patient patient);
    Task<bool> DeletePatientAsync(Guid patientId);
    Task<IEnumerable<Patient>> SearchPatientsAsync(Guid clinicId, string searchTerm);
    Task<bool> PatientExistsAsync(Guid clinicId, string fiscalCode);
}