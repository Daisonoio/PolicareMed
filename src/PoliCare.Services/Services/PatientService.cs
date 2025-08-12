using Microsoft.Extensions.Logging;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;

namespace PoliCare.Services.Services;

public class PatientService : IPatientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IUnitOfWork unitOfWork, ILogger<PatientService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<Patient>> GetPatientsByClinicAsync(Guid clinicId)
    {
        try
        {
            var patients = await _unitOfWork.Repository<Patient>()
                .GetWhereAsync(p => p.ClinicId == clinicId);

            return patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<Patient?> GetPatientByIdAsync(Guid patientId)
    {
        try
        {
            return await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<Patient> CreatePatientAsync(Patient patient)
    {
        try
        {
            // Verifica se esiste già un paziente con lo stesso codice fiscale nella clinica
            var existingPatient = await _unitOfWork.Repository<Patient>()
                .GetFirstOrDefaultAsync(p => p.ClinicId == patient.ClinicId && p.FiscalCode == patient.FiscalCode);

            if (existingPatient != null)
            {
                throw new InvalidOperationException($"A patient with fiscal code {patient.FiscalCode} already exists in this clinic");
            }

            await _unitOfWork.Repository<Patient>().AddAsync(patient);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Created patient {PatientId} for clinic {ClinicId}", patient.Id, patient.ClinicId);
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient for clinic {ClinicId}", patient.ClinicId);
            throw;
        }
    }

    public async Task<Patient?> UpdatePatientAsync(Guid patientId, Patient updatedPatient)
    {
        try
        {
            var existingPatient = await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
            if (existingPatient == null)
            {
                return null;
            }

            // Verifica codice fiscale duplicato (escluso paziente corrente)
            if (updatedPatient.FiscalCode != existingPatient.FiscalCode)
            {
                var duplicatePatient = await _unitOfWork.Repository<Patient>()
                    .GetFirstOrDefaultAsync(p => p.ClinicId == existingPatient.ClinicId &&
                                                p.FiscalCode == updatedPatient.FiscalCode &&
                                                p.Id != patientId);

                if (duplicatePatient != null)
                {
                    throw new InvalidOperationException($"A patient with fiscal code {updatedPatient.FiscalCode} already exists in this clinic");
                }
            }

            // Aggiorna proprietà
            existingPatient.FirstName = updatedPatient.FirstName;
            existingPatient.LastName = updatedPatient.LastName;
            existingPatient.Email = updatedPatient.Email;
            existingPatient.Phone = updatedPatient.Phone;
            existingPatient.DateOfBirth = updatedPatient.DateOfBirth;
            existingPatient.FiscalCode = updatedPatient.FiscalCode;
            existingPatient.Address = updatedPatient.Address;
            existingPatient.MedicalHistory = updatedPatient.MedicalHistory;
            existingPatient.Preferences = updatedPatient.Preferences;

            _unitOfWork.Repository<Patient>().Update(existingPatient);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Updated patient {PatientId}", patientId);
            return existingPatient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<bool> DeletePatientAsync(Guid patientId)
    {
        try
        {
            var result = await _unitOfWork.Repository<Patient>().DeleteAsync(patientId);
            if (result)
            {
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Deleted patient {PatientId}", patientId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<IEnumerable<Patient>> SearchPatientsAsync(Guid clinicId, string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetPatientsByClinicAsync(clinicId);
            }

            var lowerSearchTerm = searchTerm.ToLower();

            var patients = await _unitOfWork.Repository<Patient>()
                .GetWhereAsync(p => p.ClinicId == clinicId &&
                              (p.FirstName.ToLower().Contains(lowerSearchTerm) ||
                               p.LastName.ToLower().Contains(lowerSearchTerm) ||
                               p.FiscalCode.ToLower().Contains(lowerSearchTerm) ||
                               p.Email.ToLower().Contains(lowerSearchTerm)));

            return patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching patients for clinic {ClinicId} with term {SearchTerm}", clinicId, searchTerm);
            throw;
        }
    }

    public async Task<bool> PatientExistsAsync(Guid clinicId, string fiscalCode)
    {
        try
        {
            return await _unitOfWork.Repository<Patient>()
                .ExistsAsync(p => p.ClinicId == clinicId && p.FiscalCode == fiscalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking patient existence for clinic {ClinicId} and fiscal code {FiscalCode}", clinicId, fiscalCode);
            throw;
        }
    }
}