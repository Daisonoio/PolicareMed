using PoliCare.Core.Entities;
using AutoFixture;

namespace PoliCare.Tests.Helpers;

/// <summary>
/// Builder pattern per creare dati di test personalizzati
/// Utilizza AutoFixture per generazione automatica con override specifici
/// </summary>
public class TestDataBuilder
{
    private readonly Fixture _fixture;

    public TestDataBuilder()
    {
        _fixture = new Fixture();
        ConfigureFixture();
    }

    private void ConfigureFixture()
    {
        // Configurazioni personalizzate per AutoFixture
        _fixture.Customize<DateTime>(c => c.FromFactory(() => DateTime.UtcNow));
        _fixture.Customize<Guid>(c => c.FromFactory(Guid.NewGuid));

        // Configurazioni per evitare loop infiniti
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
               .ToList()
               .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    #region Clinic Builder

    public ClinicBuilder CreateClinic()
    {
        return new ClinicBuilder(_fixture);
    }

    public class ClinicBuilder
    {
        private readonly Fixture _fixture;
        private readonly Clinic _clinic;

        public ClinicBuilder(Fixture fixture)
        {
            _fixture = fixture;
            _clinic = _fixture.Build<Clinic>()
                .With(c => c.Id, Guid.NewGuid())
                .With(c => c.CreatedAt, DateTime.UtcNow)
                .With(c => c.UpdatedAt, DateTime.UtcNow)
                .With(c => c.IsDeleted, false)
                .Without(c => c.DeletedAt)
                .Without(c => c.DeletedBy)
                .Create();
        }

        public ClinicBuilder WithName(string name)
        {
            _clinic.Name = name;
            return this;
        }

        public ClinicBuilder WithAddress(string address)
        {
            _clinic.Address = address;
            return this;
        }

        public ClinicBuilder WithEmail(string email)
        {
            _clinic.Email = email;
            return this;
        }

        public ClinicBuilder WithPhone(string phone)
        {
            _clinic.Phone = phone;
            return this;
        }

        public Clinic Build() => _clinic;
    }

    #endregion

    #region User Builder

    public UserBuilder CreateUser()
    {
        return new UserBuilder(_fixture);
    }

    public class UserBuilder
    {
        private readonly Fixture _fixture;
        private readonly User _user;

        public UserBuilder(Fixture fixture)
        {
            _fixture = fixture;
            _user = _fixture.Build<User>()
                .With(u => u.Id, Guid.NewGuid())
                .With(u => u.Role, UserRole.Patient)
                .With(u => u.IsActive, true)
                .With(u => u.IsBlocked, false)
                .With(u => u.FailedLoginAttempts, 0)
                .With(u => u.MustChangePassword, false)
                .With(u => u.PasswordHash, "$2a$11$validHashForTestingPurposes123456789")
                .With(u => u.PasswordSalt, "test_salt")
                .With(u => u.CreatedAt, DateTime.UtcNow)
                .With(u => u.UpdatedAt, DateTime.UtcNow)
                .With(u => u.IsDeleted, false)
                .Without(u => u.DeletedAt)
                .Without(u => u.DeletedBy)
                .Without(u => u.LastLoginAt)
                .Without(u => u.LastFailedLoginAt)
                .Without(u => u.BlockedAt)
                .Without(u => u.BlockExpiresAt)
                .Create();
        }

        public UserBuilder WithRole(UserRole role)
        {
            _user.Role = role;
            return this;
        }

        public UserBuilder WithEmail(string email)
        {
            _user.Email = email;
            return this;
        }

        public UserBuilder WithName(string firstName, string lastName)
        {
            _user.FirstName = firstName;
            _user.LastName = lastName;
            return this;
        }

        public UserBuilder WithClinic(Guid clinicId)
        {
            _user.ClinicId = clinicId;
            return this;
        }

        public UserBuilder Blocked(string reason = "Test block")
        {
            _user.IsBlocked = true;
            _user.BlockReason = reason;
            _user.BlockedAt = DateTime.UtcNow;
            return this;
        }

        public User Build() => _user;
    }

    #endregion

    #region Doctor Builder

    public DoctorBuilder CreateDoctor()
    {
        return new DoctorBuilder(_fixture);
    }

    public class DoctorBuilder
    {
        private readonly Fixture _fixture;
        private readonly Doctor _doctor;

        public DoctorBuilder(Fixture fixture)
        {
            _fixture = fixture;
            _doctor = _fixture.Build<Doctor>()
                .With(d => d.Id, Guid.NewGuid())
                .With(d => d.User.IsActive, true)
                .With(d => d.Specialization, "Medicina Generale")
                .With(d => d.LicenseNumber, _fixture.Create<string>()[..10])
                .With(d => d.CreatedAt, DateTime.UtcNow)
                .With(d => d.UpdatedAt, DateTime.UtcNow)
                .With(d => d.IsDeleted, false)
                .Without(d => d.DeletedAt)
                .Without(d => d.DeletedBy)
                .Create();
        }

        public DoctorBuilder WithSpecialization(string specialization)
        {
            _doctor.Specialization = specialization;
            return this;
        }

        public DoctorBuilder WithLicense(string licenseNumber)
        {
            _doctor.LicenseNumber = licenseNumber;
            return this;
        }

        public DoctorBuilder WithUser(Guid userId)
        {
            _doctor.UserId = userId;
            return this;
        }

        public DoctorBuilder WithClinic(Guid clinicId)
        {
            _doctor.ClinicId = clinicId;
            return this;
        }

        public DoctorBuilder WithName(string firstName, string lastName)
        {
            _doctor.User.FirstName = firstName;
            _doctor.User.LastName = lastName;
            return this;
        }

        public Doctor Build() => _doctor;
    }

    #endregion

    #region Patient Builder

    public PatientBuilder CreatePatient()
    {
        return new PatientBuilder(_fixture);
    }

    public class PatientBuilder
    {
        private readonly Fixture _fixture;
        private readonly Patient _patient;

        public PatientBuilder(Fixture fixture)
        {
            _fixture = fixture;
            _patient = _fixture.Build<Patient>()
                .With(p => p.Id, Guid.NewGuid())
                .With(p => p.DateOfBirth, DateTime.UtcNow.AddYears(-30))
                .With(p => p.FiscalCode, "RSSMRA80A01F205Z")
                .With(p => p.MedicalHistory, "Nessuna patologia significativa")
                .With(p => p.Preferences, "{}")
                .With(p => p.CreatedAt, DateTime.UtcNow)
                .With(p => p.UpdatedAt, DateTime.UtcNow)
                .With(p => p.IsDeleted, false)
                .Without(p => p.DeletedAt)
                .Without(p => p.DeletedBy)
                .Create();
        }

        public PatientBuilder WithName(string firstName, string lastName)
        {
            _patient.FirstName = firstName;
            _patient.LastName = lastName;
            return this;
        }

        public PatientBuilder WithEmail(string email)
        {
            _patient.Email = email;
            return this;
        }

        public PatientBuilder WithFiscalCode(string fiscalCode)
        {
            _patient.FiscalCode = fiscalCode;
            return this;
        }

        public PatientBuilder WithDateOfBirth(DateTime dateOfBirth)
        {
            _patient.DateOfBirth = dateOfBirth;
            return this;
        }

        public PatientBuilder WithClinic(Guid clinicId)
        {
            _patient.ClinicId = clinicId;
            return this;
        }

        public Patient Build() => _patient;
    }

    #endregion

    #region Room Builder

    public RoomBuilder CreateRoom()
    {
        return new RoomBuilder(_fixture);
    }

    public class RoomBuilder
    {
        private readonly Fixture _fixture;
        private readonly Room _room;

        public RoomBuilder(Fixture fixture)
        {
            _fixture = fixture;
            _room = _fixture.Build<Room>()
                .With(r => r.Id, Guid.NewGuid())
                .With(r => r.IsActive, true)
                .With(r => r.CreatedAt, DateTime.UtcNow)
                .With(r => r.UpdatedAt, DateTime.UtcNow)
                .With(r => r.IsDeleted, false)
                .Without(r => r.DeletedAt)
                .Without(r => r.DeletedBy)
                .Create();
        }

        public RoomBuilder WithName(string name)
        {
            _room.Name = name;
            return this;
        }

        public RoomBuilder WithDescription(string description)
        {
            _room.Description = description;
            return this;
        }

        public RoomBuilder WithClinic(Guid clinicId)
        {
            _room.ClinicId = clinicId;
            return this;
        }

        public RoomBuilder Inactive()
        {
            _room.IsActive = false;
            return this;
        }

        public Room Build() => _room;
    }

    #endregion

    #region Appointment Builder

    public AppointmentBuilder CreateAppointment()
    {
        return new AppointmentBuilder(_fixture);
    }

    public class AppointmentBuilder
    {
        private readonly Fixture _fixture;
        private readonly Appointment _appointment;

        public AppointmentBuilder(Fixture fixture)
        {
            _fixture = fixture;
            _appointment = _fixture.Build<Appointment>()
                .With(a => a.Id, Guid.NewGuid())
                .With(a => a.StartTime, DateTime.UtcNow.AddDays(1))
                .With(a => a.ServiceType, AppointmentType.Consultation.ToString())
                .With(a => a.Status, AppointmentStatus.Scheduled.ToString())
                .With(a => a.Notes, "Appuntamento di test")
                .With(a => a.CreatedAt, DateTime.UtcNow)
                .With(a => a.UpdatedAt, DateTime.UtcNow)
                .With(a => a.IsDeleted, false)
                .Without(a => a.DeletedAt)
                .Without(a => a.DeletedBy)
                .Without(a => a.IsDeleted)
                .Create();
        }

        public AppointmentBuilder WithPatient(Guid patientId)
        {
            _appointment.PatientId = patientId;
            return this;
        }

        public AppointmentBuilder WithDoctor(Guid doctorId)
        {
            _appointment.DoctorId = doctorId;
            return this;
        }

        public AppointmentBuilder WithRoom(Guid roomId)
        {
            _appointment.RoomId = roomId;
            return this;
        }

        public AppointmentBuilder WithClinic(Guid clinicId)
        {
            _appointment.Room.Clinic.Id = clinicId;
            return this;
        }

        public AppointmentBuilder WithDateTime(DateTime scheduledAt)
        {
            _appointment.StartTime = scheduledAt;
            return this;
        }

        public AppointmentBuilder WithType(AppointmentType type)
        {
            _appointment.ServiceType = type.ToString();
            return this;
        }

        public AppointmentBuilder WithStatus(AppointmentStatus status)
        {
            _appointment.Status = status.ToString();
            return this;
        }

        public AppointmentBuilder Cancelled(string reason = "Test cancellation")
        {
            _appointment.Status = AppointmentStatus.Cancelled.ToString();
            _appointment.DeletedBy = reason;
            _appointment.DeletedAt = DateTime.UtcNow;
            return this;
        }

        public AppointmentBuilder Completed()
        {
            _appointment.Status = AppointmentStatus.Completed.ToString();
            return this;
        }

        public Appointment Build() => _appointment;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Crea un set completo di dati correlati (Clinic, User, Doctor, Room, Patient)
    /// </summary>
    public CompleteTestDataSet CreateCompleteDataSet()
    {
        var clinic = CreateClinic()
            .WithName("Test Clinic Complete")
            .WithEmail("complete@test.com")
            .Build();

        var doctorUser = CreateUser()
            .WithRole(UserRole.Doctor)
            .WithEmail("doctor@test.com")
            .WithName("Dr. Test", "Doctor")
            .WithClinic(clinic.Id)
            .Build();

        var doctor = CreateDoctor()
            .WithUser(doctorUser.Id)
            .WithClinic(clinic.Id)
            .WithName("Test", "Doctor")
            .WithSpecialization("Test Medicine")
            .Build();

        var room = CreateRoom()
            .WithName("Test Room")
            .WithClinic(clinic.Id)
            .Build();

        var patient = CreatePatient()
            .WithName("Test", "Patient")
            .WithEmail("patient@test.com")
            .WithClinic(clinic.Id)
            .Build();

        var appointment = CreateAppointment()
            .WithClinic(clinic.Id)
            .WithDoctor(doctor.Id)
            .WithPatient(patient.Id)
            .WithRoom(room.Id)
            .WithDateTime(DateTime.UtcNow.AddDays(1).Date.AddHours(10))
            .Build();

        return new CompleteTestDataSet
        {
            Clinic = clinic,
            DoctorUser = doctorUser,
            Doctor = doctor,
            Room = room,
            Patient = patient,
            Appointment = appointment
        };
    }

    /// <summary>
    /// Crea dati per test Smart Scheduling con più opzioni
    /// </summary>
    public SmartSchedulingTestData CreateSmartSchedulingData()
    {
        var clinic = CreateClinic()
            .WithName("Smart Scheduling Test Clinic")
            .Build();

        // Crea 3 dottori con diverse specializzazioni
        var doctors = new List<Doctor>();
        var doctorUsers = new List<User>();

        for (int i = 1; i <= 3; i++)
        {
            var user = CreateUser()
                .WithRole(UserRole.Doctor)
                .WithEmail($"doctor{i}@test.com")
                .WithName($"Dr. Test{i}", "Doctor")
                .WithClinic(clinic.Id)
                .Build();

            var doctor = CreateDoctor()
                .WithUser(user.Id)
                .WithClinic(clinic.Id)
                .WithName($"Test{i}", "Doctor")
                .WithSpecialization($"Specialization {i}")
                .Build();

            doctorUsers.Add(user);
            doctors.Add(doctor);
        }

        // Crea 3 sale
        var rooms = new List<Room>();
        for (int i = 1; i <= 3; i++)
        {
            var room = CreateRoom()
                .WithName($"Test Room {i}")
                .WithClinic(clinic.Id)
                .Build();

            rooms.Add(room);
        }

        // Crea 5 pazienti
        var patients = new List<Patient>();
        for (int i = 1; i <= 5; i++)
        {
            var patient = CreatePatient()
                .WithName($"Test{i}", "Patient")
                .WithEmail($"patient{i}@test.com")
                .WithClinic(clinic.Id)
                .Build();

            patients.Add(patient);
        }

        // Crea alcuni appuntamenti esistenti per test conflitti
        var existingAppointments = new List<Appointment>();
        var tomorrow10AM = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        var appointment1 = CreateAppointment()
            .WithClinic(clinic.Id)
            .WithDoctor(doctors[0].Id)
            .WithPatient(patients[0].Id)
            .WithRoom(rooms[0].Id)
            .WithDateTime(tomorrow10AM)
            .Build();

        var appointment2 = CreateAppointment()
            .WithClinic(clinic.Id)
            .WithDoctor(doctors[1].Id)
            .WithPatient(patients[1].Id)
            .WithRoom(rooms[1].Id)
            .WithDateTime(tomorrow10AM.AddHours(1))
            .Build();

        existingAppointments.Add(appointment1);
        existingAppointments.Add(appointment2);

        return new SmartSchedulingTestData
        {
            Clinic = clinic,
            DoctorUsers = doctorUsers,
            Doctors = doctors,
            Rooms = rooms,
            Patients = patients,
            ExistingAppointments = existingAppointments
        };
    }

    #endregion
}

/// <summary>
/// Set completo di dati di test correlati
/// </summary>
public class CompleteTestDataSet
{
    public Clinic Clinic { get; set; } = null!;
    public User DoctorUser { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;

    public IEnumerable<Guid> GetAllIds()
    {
        yield return Clinic.Id;
        yield return DoctorUser.Id;
        yield return Doctor.Id;
        yield return Room.Id;
        yield return Patient.Id;
        yield return Appointment.Id;
    }
}

/// <summary>
/// Dati specifici per test Smart Scheduling
/// </summary>
public class SmartSchedulingTestData
{
    public Clinic Clinic { get; set; } = null!;
    public List<User> DoctorUsers { get; set; } = new();
    public List<Doctor> Doctors { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<Patient> Patients { get; set; } = new();
    public List<Appointment> ExistingAppointments { get; set; } = new();

    public IEnumerable<Guid> GetAllIds()
    {
        yield return Clinic.Id;

        foreach (var user in DoctorUsers)
            yield return user.Id;

        foreach (var doctor in Doctors)
            yield return doctor.Id;

        foreach (var room in Rooms)
            yield return room.Id;

        foreach (var patient in Patients)
            yield return patient.Id;

        foreach (var appointment in ExistingAppointments)
            yield return appointment.Id;
    }
}