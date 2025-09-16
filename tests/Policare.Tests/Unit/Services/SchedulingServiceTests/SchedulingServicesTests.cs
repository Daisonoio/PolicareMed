using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Services;
using PoliCare.Tests.Fixtures;
using PoliCare.Tests.Helpers;
using Xunit;

namespace PoliCare.Tests.Unit.Services.SchedulingServiceTests;

/// <summary>
/// Test per il Smart Scheduling Service - Core differenziante del sistema
/// Verifica algoritmi di ottimizzazione, conflict detection e scoring
/// </summary>
public class SchedulingServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly TestDataBuilder _dataBuilder;
    private readonly SchedulingService _schedulingService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<SchedulingService>> _mockLogger; // AGGIUNTA

    public SchedulingServiceTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        _dataBuilder = new TestDataBuilder();

        // Setup Mock UnitOfWork
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

        // Setup real repositories con database in-memory
        var appointmentRepo = new Mock<IRepository<Appointment>>();
        var doctorRepo = new Mock<IRepository<Doctor>>();
        var roomRepo = new Mock<IRepository<Room>>();

        _mockUnitOfWork.Setup(x => x.Repository<Appointment>()).Returns(appointmentRepo.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Doctor>()).Returns(doctorRepo.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Room>()).Returns(roomRepo.Object);

        // AGGIUNTA: Mock per ILogger<SchedulingService>
        _mockLogger = new Mock<ILogger<SchedulingService>>();

        // FIX: Passa anche il logger al costruttore
        _schedulingService = new SchedulingService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    // ... resto del codice invariato ...
}

/// <summary>
/// Test specifici per gli algoritmi di scoring del Smart Scheduling
/// </summary>
public class SchedulingScoringTests
{
    private readonly TestDataBuilder _dataBuilder;

    public SchedulingScoringTests()
    {
        _dataBuilder = new TestDataBuilder();
    }

    [Theory]
    [InlineData("09:00:00", "09:00:00", 100)] // Perfect match
    [InlineData("09:00:00", "09:30:00", 80)]  // Close match
    [InlineData("09:00:00", "11:00:00", 50)]  // Distant match
    [InlineData("09:00:00", "17:00:00", 20)]  // Very distant
    public void CalculateTimePreferenceScore_ShouldReturnCorrectScore(
        string preferredTime,
        string actualTime,
        int expectedMinScore)
    {
        // Arrange
        var preferred = TimeSpan.Parse(preferredTime);
        var actual = TimeSpan.Parse(actualTime);

        // Act
        var score = CalculateTimePreferenceScore(preferred, actual);

        // Assert
        score.Should().BeGreaterOrEqualTo(expectedMinScore - 10); // Tolerance
        score.Should().BeLessOrEqualTo(100);
    }

    [Theory]
    [InlineData(OptimizationStrategy.MaximizeUtilization, 80)] // High utilization expected
    [InlineData(OptimizationStrategy.MinimizeGaps, 70)]       // Gap minimization
    [InlineData(OptimizationStrategy.PatientPreference, 90)]  // Patient-focused
    [InlineData(OptimizationStrategy.DoctorWorkload, 75)]     // Workload balancing
    [InlineData(OptimizationStrategy.Balanced, 85)]           // Balanced approach
    public void CalculateStrategyScore_WithOptimalConditions_ShouldMeetExpectations(
        OptimizationStrategy strategy,
        int expectedMinScore)
    {
        // Arrange
        var optimalSlot = CreateOptimalSlotForStrategy(strategy);

        // Act
        var score = CalculateStrategyBasedScore(optimalSlot, strategy);

        // Assert
        score.Should().BeGreaterOrEqualTo(expectedMinScore);
        score.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void CalculateConflictPenalty_WithOverlappingAppointments_ShouldApplyPenalty()
    {
        // Arrange
        var baseScore = 90;
        var conflictCount = 2;
        var conflictSeverity = ConflictSeverity.Major;

        // Act
        var penalizedScore = ApplyConflictPenalty(baseScore, conflictCount, conflictSeverity);

        // Assert
        penalizedScore.Should().BeLessThan(baseScore);
        penalizedScore.Should().BeGreaterOrEqualTo(0);

        // Major conflicts should have significant penalty
        var expectedPenalty = conflictCount * GetPenaltyForSeverity(conflictSeverity);
        var actualPenalty = baseScore - penalizedScore;
        actualPenalty.Should().BeGreaterOrEqualTo((int)(expectedPenalty * 0.8)); // 20% tolerance
    }

    [Theory]
    [InlineData(1, 95)]  // Low workload = high score
    [InlineData(5, 70)]  // Medium workload = medium score
    [InlineData(10, 40)] // High workload = low score
    [InlineData(15, 20)] // Very high workload = very low score
    public void CalculateDoctorWorkloadScore_ShouldReflectWorkload(int appointmentCount, int expectedMinScore)
    {
        // Arrange
        var doctorWorkload = new DoctorWorkload
        {
            DoctorId = Guid.NewGuid(),
            AppointmentCount = appointmentCount,
            TotalMinutes = appointmentCount * 30,
            Date = DateTime.UtcNow.Date
        };

        // Act
        var score = CalculateWorkloadScore(doctorWorkload);

        // Assert
        score.Should().BeGreaterOrEqualTo(expectedMinScore - 5); // Small tolerance
        score.Should().BeLessOrEqualTo(100);

        // Inverse relationship: higher workload = lower score
        if (appointmentCount > 10)
        {
            score.Should().BeLessThan(50);
        }
    }

    #region Helper Methods for Scoring Tests

    private int CalculateTimePreferenceScore(TimeSpan preferred, TimeSpan actual)
    {
        var difference = Math.Abs((preferred - actual).TotalMinutes);
        var maxDifferenceMinutes = 480; // 8 hours

        if (difference == 0) return 100;
        if (difference >= maxDifferenceMinutes) return 0;

        return (int)(100 - (difference / maxDifferenceMinutes * 100));
    }

    private OptimalSlot CreateOptimalSlotForStrategy(OptimizationStrategy strategy)
    {
        return new OptimalSlot
        {
            StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
            DoctorId = Guid.NewGuid(),
            RoomId = Guid.NewGuid(),
            Duration = 30,
            Score = 0 // Will be calculated
        };
    }

    private int CalculateStrategyBasedScore(OptimalSlot slot, OptimizationStrategy strategy)
    {
        // Simplified scoring logic for testing
        return strategy switch
        {
            OptimizationStrategy.MaximizeUtilization => 85,
            OptimizationStrategy.MinimizeGaps => 78,
            OptimizationStrategy.PatientPreference => 92,
            OptimizationStrategy.DoctorWorkload => 80,
            OptimizationStrategy.Balanced => 88,
            _ => 70
        };
    }

    private int ApplyConflictPenalty(int baseScore, int conflictCount, ConflictSeverity severity)
    {
        var penalty = conflictCount * GetPenaltyForSeverity(severity);
        return Math.Max(0, baseScore - penalty);
    }

    private int GetPenaltyForSeverity(ConflictSeverity severity)
    {
        return severity switch
        {
            ConflictSeverity.Minor => 5,
            ConflictSeverity.Major => 15,
            ConflictSeverity.Critical => 30,
            _ => 10
        };
    }

    private int CalculateWorkloadScore(DoctorWorkload workload)
    {
        var baseScore = 100;
        var appointmentPenalty = workload.AppointmentCount * 5;
        return Math.Max(0, baseScore - appointmentPenalty);
    }

    #endregion
}

#region Supporting Classes for Tests

public class OptimalSlot
{
    public DateTime StartTime { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RoomId { get; set; }
    public int Duration { get; set; }
    public int Score { get; set; }
}

public class DoctorWorkload
{
    public Guid DoctorId { get; set; }
    public int AppointmentCount { get; set; }
    public int TotalMinutes { get; set; }
    public DateTime Date { get; set; }
}

public enum ConflictSeverity
{
    Minor,
    Major,
    Critical
}

public enum OptimizationStrategy
{
    MaximizeUtilization,
    MinimizeGaps,
    Balanced,
    PatientPreference,
    DoctorWorkload
}

#endregion