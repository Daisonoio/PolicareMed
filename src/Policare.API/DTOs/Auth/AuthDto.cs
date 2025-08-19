using System.ComponentModel.DataAnnotations;
using PoliCare.Core.Entities;

namespace Policare.API.DTOs;

/// <summary>
/// DTO per richiesta di login
/// </summary>
public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Mantieni la sessione attiva più a lungo
    /// </summary>
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// Informazioni del dispositivo per tracking sicurezza
    /// </summary>
    public string? DeviceInfo { get; set; }
}

/// <summary>
/// DTO per richiesta di registrazione
/// </summary>
public class RegisterRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// ID della clinica di appartenenza
    /// </summary>
    [Required]
    public Guid ClinicId { get; set; }

    /// <summary>
    /// Ruolo dell'utente (default: Receptionist)
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Receptionist;

    /// <summary>
    /// Timezone preferito
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>
    /// Lingua preferita
    /// </summary>
    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "it-IT";
}

/// <summary>
/// Risposta di autenticazione con token JWT
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Token JWT per autenticazione
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token per rinnovo automatico
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Scadenza del token (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Tipo di token (sempre "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Informazioni dell'utente autenticato
    /// </summary>
    public UserInfoDto User { get; set; } = null!;

    /// <summary>
    /// Informazioni della sessione
    /// </summary>
    public SessionInfoDto Session { get; set; } = null!;
}

/// <summary>
/// Informazioni utente per risposta auth
/// </summary>
public class UserInfoDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleDisplayName => Role.GetDisplayName();
    public Guid ClinicId { get; set; }
    public Guid? PrimaryClinicId { get; set; }
    public string TimeZone { get; set; } = "Europe/Rome";
    public string PreferredLanguage { get; set; } = "it-IT";
    public DateTime? LastLoginAt { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

/// <summary>
/// Informazioni sessione per risposta auth
/// </summary>
public class SessionInfoDto
{
    public Guid SessionId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? DeviceType { get; set; }
    public string? IPAddress { get; set; }
    public string? Location { get; set; }
    public bool IsNewDevice { get; set; }
    public bool IsNewLocation { get; set; }
}

/// <summary>
/// Richiesta refresh token
/// </summary>
public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta cambio password
/// </summary>
public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta reset password
/// </summary>
public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta reset password con token
/// </summary>
public class ResetPasswordRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Risposta per operazioni che richiedono solo conferma
/// </summary>
public class AuthConfirmationDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// DTO per informazioni sessione attiva
/// </summary>
public class ActiveSessionDto
{
    public Guid SessionId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? IPAddress { get; set; }
    public string? Location { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsSuspicious { get; set; }
    public string? SuspiciousReason { get; set; }
    public int RequestCount { get; set; }
}