namespace Policare.API.DTOs;

public class DoctorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ClinicId { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal CommissionPercentage { get; set; }

    // User data (nested)
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CreateDoctorDto
{
    public Guid ClinicId { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal CommissionPercentage { get; set; } = 0.7m;

    // User data for creation
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // Will be hashed
}

public class UpdateDoctorDto
{
    public string Specialization { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal CommissionPercentage { get; set; }

    // User updates (optional - solo alcuni campi)
    public string? Phone { get; set; }
}

public class DoctorAvailabilityDto
{
    public Guid DoctorId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string? ConflictReason { get; set; }
}