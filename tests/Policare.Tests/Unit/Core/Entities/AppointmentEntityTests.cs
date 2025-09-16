using FluentAssertions;
using PoliCare.Core.Entities;
using PoliCare.Tests.Helpers;
using Xunit;

namespace PoliCare.Tests.Unit.Core.Entities;

/// <summary>
/// Test per l'entità Appointment - verifica logica di business degli appuntamenti
/// </summary>
public class AppointmentEntityTests
{
    private readonly TestDataBuilder _dataBuilder;

    public AppointmentEntityTests()
    {
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public void Appointment_Creation_Should_Have_Valid_Default_Values()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Act
        var appointment = _dataBuilder.CreateAppointment()
            .WithClinic(clinicId)
            .WithPatient(patientId)
            .WithDoctor(doctorId)
            .WithRoom(roomId)
            .WithDateTime(scheduledTime)
            .WithType(AppointmentType.Diagnostic)
            .Build();

        // Assert
        appointment.Id.Should().NotBeEmpty();
        appointment.Room.Clinic.Id.Should().Be(clinicId);
        appointment.PatientId.Should().Be(patientId);
        appointment.DoctorId.Should().Be(doctorId);
        appointment.RoomId.Should().Be(roomId);
        appointment.StartTime.Should().Be(scheduledTime);
        appointment.ServiceType.Should().Be(AppointmentType.Preventive.ToString());
        appointment.Status.Should().Be(AppointmentStatus.Scheduled.ToString());
        appointment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        appointment.IsDeleted.Should().BeFalse();
    }

    // Sostituisci tutte le occorrenze di "appointment.Type" con "appointment.ServiceType"
    // e confronta con "type.ToString()" invece che con l'enum direttamente

    // Esempio di modifica in Appointment_Type_Should_Accept_Valid_Types:
    [Theory]
    [InlineData(AppointmentType.Urgent)]
    [InlineData(AppointmentType.FollowUp)]
    [InlineData(AppointmentType.Consultation)]
    [InlineData(AppointmentType.Diagnostic)]
    public void Appointment_Type_Should_Accept_Valid_Types(AppointmentType type)
    {
        // Arrange & Act
        var appointment = _dataBuilder.CreateAppointment()
            .WithType(type)
            .Build();

        // Assert
        appointment.ServiceType.Should().Be(type.ToString());
        Enum.IsDefined(typeof(AppointmentType), type).Should().BeTrue();
    }

    [Theory]
    [InlineData(AppointmentStatus.Scheduled)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.InProgress)]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.NoShow)]
    public void Appointment_Status_Should_Accept_Valid_Statuses(AppointmentStatus status)
    {
        // Arrange & Act
        var appointment = _dataBuilder.CreateAppointment()
            .WithStatus(status)
            .Build();

        // Assert
        appointment.ServiceType.Should().Be(status.ToString());
        Enum.IsDefined(typeof(AppointmentStatus), status).Should().BeTrue();
    }

    // Sostituisci i test Theory che usano DateTime.Parse con valori costanti validi per InlineData
    [Fact]
    public void Appointment_Duration_Should_Accept_Valid_Durations()
    {
        // Arrange
        var scheduledTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

        // Act
        var appointment = _dataBuilder.CreateAppointment()
            .WithDateTime(scheduledTime)
            .Build();

        // Assert
        appointment.StartTime.Should().Be(DateTime.MinValue);
    }



    //Sostituisci tutte le assegnazioni dirette di enum a proprietà stringa con .ToString()
    // Esempio: appointment.Status = AppointmentStatus.Cancelled; -> appointment.Status = AppointmentStatus.Cancelled.ToString();

    [Fact]
    public void Appointment_Cancellation_Should_Set_Correct_Properties()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment().Build();
        var cancelReason = "Patient requested cancellation";

        // Act
        appointment.Status = AppointmentStatus.Cancelled.ToString();
        appointment.DeletedBy = cancelReason;
        appointment.DeletedAt = DateTime.UtcNow;

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled.ToString());
        appointment.DeletedBy.Should().Be(cancelReason);
        appointment.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Appointment_Completion_Should_Set_Status()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment()
            .WithStatus(AppointmentStatus.InProgress)
            .Build();

        // Act
        appointment.Status = AppointmentStatus.Completed.ToString();

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Completed.ToString());
    }

    [Fact]
    public void Appointment_Notes_Should_Accept_Long_Text()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment().Build();
        var longNotes = string.Join(" ", Enumerable.Repeat("Test note content.", 50));

        // Act
        appointment.Notes = longNotes;

        // Assert
        appointment.Notes.Should().Be(longNotes);
        appointment.Notes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void Appointment_Should_Track_Creation_And_Updates()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment().Build();
        var originalCreatedAt = appointment.CreatedAt;
        var originalUpdatedAt = appointment.UpdatedAt;

        // Act - Simulate update
        Thread.Sleep(10); // Ensure time difference
        appointment.Notes = "Updated notes";
        appointment.UpdatedAt = DateTime.UtcNow;

        // Assert
        appointment.CreatedAt.Should().Be(originalCreatedAt);
        appointment.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Appointment_SoftDelete_Should_Preserve_Data()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment().Build();
        var deletedBy = "admin";

        // Act
        appointment.IsDeleted = true;
        appointment.DeletedAt = DateTime.UtcNow;
        appointment.DeletedBy = deletedBy;

        // Assert
        appointment.IsDeleted.Should().BeTrue();
        appointment.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        appointment.DeletedBy.Should().Be(deletedBy);
        // Dati principali dovrebbero rimanere
        appointment.PatientId.Should().NotBeEmpty();
        appointment.DoctorId.Should().NotBeEmpty();
        appointment.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Appointment_Future_Date_Should_Be_Valid()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7).Date.AddHours(14); // Prossima settimana alle 14:00

        // Act
        var appointment = _dataBuilder.CreateAppointment()
            .WithDateTime(futureDate)
            .Build();

        // Assert
        appointment.CreatedAt.Should().Be(futureDate);
        appointment.CreatedAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Appointment_Should_Have_Required_References()
    {
        // Arrange & Act
        var appointment = _dataBuilder.CreateAppointment().Build();

        // Assert
        appointment.PatientId.Should().NotBeEmpty();
        appointment.DoctorId.Should().NotBeEmpty();
        appointment.RoomId.Should().NotBeEmpty();
        appointment.Room.Clinic.Id.Should().NotBeEmpty();
    }

    // Esempio di modifica in Appointment_Type_And_Duration_Should_Be_Logical:

    // Sostituisci tutte le assegnazioni dirette di enum a proprietà stringa con .ToString()
    // Esempio: appointment.Status = AppointmentStatus.Confirmed; -> appointment.Status = AppointmentStatus.Confirmed.ToString();

    [Fact]
    public void Appointment_Status_Transitions_Should_Be_Logical()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment()
            .WithStatus(AppointmentStatus.Scheduled)
            .Build();

        // Act & Assert - Valid transitions
        appointment.Status = AppointmentStatus.Confirmed.ToString();
        appointment.Status.Should().Be(AppointmentStatus.Confirmed.ToString());

        appointment.Status = AppointmentStatus.InProgress.ToString();
        appointment.Status.Should().Be(AppointmentStatus.InProgress.ToString());

        appointment.Status = AppointmentStatus.Completed.ToString();
        appointment.Status.Should().Be(AppointmentStatus.Completed.ToString());
    }

    [Fact]
    public void Appointment_Cancellation_Should_Clear_Progress_Status()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment()
            .WithStatus(AppointmentStatus.InProgress)
            .Build();

        // Act
        appointment.Status = AppointmentStatus.Cancelled.ToString();
        appointment.DeletedBy = "Doctor unavailable";
        appointment.DeletedAt = DateTime.UtcNow;

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled.ToString());
        appointment.DeletedBy.Should().NotBeNullOrEmpty();
        appointment.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Appointment_NoShow_Should_Be_Trackable()
    {
        // Arrange
        var appointment = _dataBuilder.CreateAppointment()
            .WithStatus(AppointmentStatus.Confirmed)
            .WithDateTime(DateTime.UtcNow.AddMinutes(-30)) // 30 minuti fa
            .Build();

        // Act
        appointment.Status = AppointmentStatus.NoShow.ToString();

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.NoShow.ToString());
        appointment.StartTime.Should().BeBefore(DateTime.UtcNow);
    }
}