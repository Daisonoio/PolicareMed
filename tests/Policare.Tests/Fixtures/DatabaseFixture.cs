using Microsoft.EntityFrameworkCore;
using PoliCare.Infrastructure.Data;
using PoliCare.Core.Entities;

namespace PoliCare.Tests.Fixtures;

/// <summary>
/// Fixture per setup database in-memory per i test
/// Implementa IDisposable per cleanup automatico
/// </summary>
public class DatabaseFixture : IDisposable
{
    public PoliCareDbContext Context { get; private set; }
    public string DatabaseName { get; private set; }

    /// <summary>
    /// Dati di test preconfigurati
    /// </summary>
    public TestData TestData { get; private set; }

    public DatabaseFixture()
    {
        DatabaseName = $"TestDB_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<PoliCareDbContext>()
            .UseInMemoryDatabase(databaseName: DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new PoliCareDbContext(options);

        // Ensure database is created
        Context.Database.EnsureCreated();

        // Seed test data
        TestData = SeedTestData();
    }

    /// <summary>
    /// Crea un nuovo context isolato per test specifici
    /// </summary>
    public PoliCareDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<PoliCareDbContext>()
            .UseInMemoryDatabase(databaseName: DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new PoliCareDbContext(options);
    }

    /// <summary>
    /// Resetta il database rimuovendo tutti i dati
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        // Remove all data but keep schema
        var entityTypes = Context.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                await Context.Database.ExecuteSqlRawAsync($"DELETE FROM {tableName}");
            }
        }

        await Context.SaveChangesAsync();

        // Re-seed test data
        TestData = SeedTestData();
    }

    /// <summary>
    /// Popola il database con dati di test predefiniti
    /// </summary>
    private TestData SeedTestData()
    {
        var testData = new TestData();

        // 1. Crea Clinica Test
        testData.TestClinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Name = "Test Poliambulatorio",
            Address = "Via Test 123, Milano",
            Phone = "+39 02 1234567",
            Email = "test@policare.test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Clinics.Add(testData.TestClinic);

        // 2. Crea User Test per Doctor
        testData.TestDoctorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Dr. Mario",
            LastName = "Rossi",
            Email = "mario.rossi@policare.test",
            Phone = "+39 334 1234567",
            PasswordHash = "$2a$11$abcdefghijklmnopqrstuvwxyz1234567890", // Hash di "TestPassword123!"
            PasswordSalt = "test_salt_123",
            Role = UserRole.Doctor,
            ClinicId = testData.TestClinic.Id,
            IsActive = true,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Users.Add(testData.TestDoctorUser);

        // 3. Crea Doctor Test
        testData.TestDoctor = new Doctor
        {
            Id = Guid.NewGuid(),
            User = testData.TestDoctorUser,
            Specialization = "Medicina Generale",
            LicenseNumber = "MED123456",
            ClinicId = testData.TestClinic.Id,
            UserId = testData.TestDoctorUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Doctors.Add(testData.TestDoctor);

        // 4. Crea Room Test
        testData.TestRoom = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Sala Test 1",
            Description = "Sala principale per visite mediche test",
            ClinicId = testData.TestClinic.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Rooms.Add(testData.TestRoom);

        // 5. Crea Patient Test
        testData.TestPatient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Luigi",
            LastName = "Bianchi",
            Email = "luigi.bianchi@test.com",
            Phone = "+39 335 7654321",
            DateOfBirth = new DateTime(1980, 5, 15),
            FiscalCode = "BNCLGU80E15F205Z",
            Address = "Via Verdi 456, Milano",
            MedicalHistory = "Nessuna patologia significativa",
            Preferences = "{}",
            ClinicId = testData.TestClinic.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Patients.Add(testData.TestPatient);

        // 6. Crea Appointment Test (per Smart Scheduling tests)
        testData.TestAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = testData.TestPatient.Id,
            DoctorId = testData.TestDoctor.Id,
            Room = testData.TestRoom,
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10), // Domani alle 10:00
            EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(11), // Domani alle 11:00
            ServiceType = AppointmentType.Consultation.ToString(),
            Status = AppointmentStatus.Scheduled.ToString(),
            Notes = "Appuntamento di test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Appointments.Add(testData.TestAppointment);

        // Salva tutti i dati
        Context.SaveChanges();

        return testData;
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}

/// <summary>
/// Classe per contenere tutti i dati di test preconfigurati
/// </summary>
public class TestData
{
    public Clinic TestClinic { get; set; } = null!;
    public User TestDoctorUser { get; set; } = null!;
    public Doctor TestDoctor { get; set; } = null!;
    public Room TestRoom { get; set; } = null!;
    public Patient TestPatient { get; set; } = null!;
    public Appointment TestAppointment { get; set; } = null!;

    /// <summary>
    /// Ottiene tutti i GUID creati per facile cleanup
    /// </summary>
    public IEnumerable<Guid> GetAllIds()
    {
        yield return TestClinic.Id;
        yield return TestDoctorUser.Id;
        yield return TestDoctor.Id;
        yield return TestRoom.Id;
        yield return TestPatient.Id;
        yield return TestAppointment.Id;
    }
}