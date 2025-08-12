using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // AGGIUNTO - Necessario per Include
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;
namespace PoliCare.Services.Services;

public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(IUnitOfWork unitOfWork, ILogger<DoctorService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<Doctor>> GetDoctorsByClinicAsync(Guid clinicId)
    {
        try
        {
            var doctors = await _unitOfWork.Repository<Doctor>()
                .Find(d => d.ClinicId == clinicId)
                .Include(d => d.User)
                .OrderBy(d => d.User.LastName)
                .ThenBy(d => d.User.FirstName)
                .ToListAsync();

            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors for clinic {ClinicId}", clinicId);
            throw;
        }
    }

    public async Task<Doctor?> GetDoctorByIdAsync(Guid doctorId)
    {
        try
        {
            var doctor = await _unitOfWork.Repository<Doctor>()
                .Find(d => d.Id == doctorId)
                .Include(d => d.User)
                .Include(d => d.Clinic)
                .FirstOrDefaultAsync();

            return doctor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<Doctor?> GetDoctorByUserIdAsync(Guid userId)
    {
        try
        {
            var doctor = await _unitOfWork.Repository<Doctor>()
                .Find(d => d.UserId == userId)
                .Include(d => d.User)
                .Include(d => d.Clinic)
                .FirstOrDefaultAsync();

            return doctor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor by user {UserId}", userId);
            throw;
        }
    }

    public async Task<Doctor> CreateDoctorAsync(Doctor doctor, User user)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // 1. Verifica unicità license number
            if (!string.IsNullOrEmpty(doctor.LicenseNumber))
            {
                var existingDoctor = await _unitOfWork.Repository<Doctor>()
                    .Find(d => d.LicenseNumber == doctor.LicenseNumber)
                    .FirstOrDefaultAsync();

                if (existingDoctor != null)
                {
                    throw new InvalidOperationException($"A doctor with license number {doctor.LicenseNumber} already exists");
                }
            }

            // 2. Verifica unicità email utente
            var existingUser = await _unitOfWork.Repository<User>()
                .Find(u => u.Email == user.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                throw new InvalidOperationException($"A user with email {user.Email} already exists");
            }

            // 3. Crea User prima
            user.Role = UserRole.Doctor;
            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.CompleteAsync();

            // 4. Crea Doctor associato
            doctor.UserId = user.Id;
            await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
            await _unitOfWork.CompleteAsync();

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Created doctor {DoctorId} with user {UserId} for clinic {ClinicId}",
                doctor.Id, user.Id, doctor.ClinicId);

            return doctor;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating doctor for clinic {ClinicId}", doctor.ClinicId);
            throw;
        }
    }

    public async Task<Doctor?> UpdateDoctorAsync(Guid doctorId, Doctor updatedDoctor)
    {
        try
        {
            var existingDoctor = await _unitOfWork.Repository<Doctor>()
                .Find(d => d.Id == doctorId)
                .Include(d => d.User)
                .FirstOrDefaultAsync();

            if (existingDoctor == null)
            {
                return null;
            }

            // Verifica license number duplicato (escluso doctor corrente)
            if (!string.IsNullOrEmpty(updatedDoctor.LicenseNumber) &&
                updatedDoctor.LicenseNumber != existingDoctor.LicenseNumber)
            {
                var duplicateDoctor = await _unitOfWork.Repository<Doctor>()
                    .Find(d => d.LicenseNumber == updatedDoctor.LicenseNumber && d.Id != doctorId)
                    .FirstOrDefaultAsync();

                if (duplicateDoctor != null)
                {
                    throw new InvalidOperationException($"A doctor with license number {updatedDoctor.LicenseNumber} already exists");
                }
            }

            // Aggiorna Doctor
            existingDoctor.Specialization = updatedDoctor.Specialization;
            existingDoctor.LicenseNumber = updatedDoctor.LicenseNumber;
            existingDoctor.HourlyRate = updatedDoctor.HourlyRate;
            existingDoctor.CommissionPercentage = updatedDoctor.CommissionPercentage;
            existingDoctor.WorkingHours = updatedDoctor.WorkingHours;

            _unitOfWork.Repository<Doctor>().Update(existingDoctor);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Updated doctor {DoctorId}", doctorId);
            return existingDoctor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<bool> DeleteDoctorAsync(Guid doctorId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Soft delete del Doctor
            var doctorResult = await _unitOfWork.Repository<Doctor>().DeleteAsync(doctorId);
            if (!doctorResult)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            // Trova e soft delete dell'User associato
            var doctor = await _unitOfWork.Repository<Doctor>()
                .FindWithDeleted(d => d.Id == doctorId)
                .FirstOrDefaultAsync();

            if (doctor != null)
            {
                await _unitOfWork.Repository<User>().DeleteAsync(doctor.UserId);
            }

            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Deleted doctor {DoctorId} and associated user", doctorId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error deleting doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(Guid clinicId, string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetDoctorsByClinicAsync(clinicId);
            }

            var lowerSearchTerm = searchTerm.ToLower();

            var doctors = await _unitOfWork.Repository<Doctor>()
                .Find(d => d.ClinicId == clinicId &&
                          (d.User.FirstName.ToLower().Contains(lowerSearchTerm) ||
                           d.User.LastName.ToLower().Contains(lowerSearchTerm) ||
                           d.Specialization.ToLower().Contains(lowerSearchTerm) ||
                           (d.LicenseNumber != null && d.LicenseNumber.ToLower().Contains(lowerSearchTerm))))
                .Include(d => d.User)
                .OrderBy(d => d.User.LastName)
                .ThenBy(d => d.User.FirstName)
                .ToListAsync();

            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching doctors for clinic {ClinicId} with term {SearchTerm}", clinicId, searchTerm);
            throw;
        }
    }

    public async Task<bool> DoctorExistsAsync(Guid clinicId, string licenseNumber)
    {
        try
        {
            return await _unitOfWork.Repository<Doctor>()
                .ExistsAsync(d => d.ClinicId == clinicId && d.LicenseNumber == licenseNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking doctor existence for clinic {ClinicId} and license {LicenseNumber}", clinicId, licenseNumber);
            throw;
        }
    }

    public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(Guid clinicId, string specialization)
    {
        try
        {
            var doctors = await _unitOfWork.Repository<Doctor>()
                .Find(d => d.ClinicId == clinicId && d.Specialization.ToLower() == specialization.ToLower())
                .Include(d => d.User)
                .OrderBy(d => d.User.LastName)
                .ThenBy(d => d.User.FirstName)
                .ToListAsync();

            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors by specialization {Specialization} for clinic {ClinicId}", specialization, clinicId);
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync(Guid doctorId, DateTime startTime, DateTime endTime)
    {
        try
        {
            // Verifica conflitti con appuntamenti esistenti
            var hasConflict = await _unitOfWork.Repository<Appointment>()
                .ExistsAsync(a => a.DoctorId == doctorId &&
                                 a.Status != AppointmentStatus.Cancelled.ToString() &&
                                 a.Status != AppointmentStatus.NoShow.ToString() &&
                                 ((a.StartTime < endTime && a.EndTime > startTime)));

            return !hasConflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking doctor {DoctorId} availability", doctorId);
            throw;
        }
    }
}