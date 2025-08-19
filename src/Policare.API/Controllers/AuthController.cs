using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;
using Policare.API.DTOs;
using System.Security.Claims;

namespace Policare.API.Controllers;

/// <summary>
/// Controller per autenticazione e gestione account utenti
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;

    public AuthController(
        ILogger<AuthController> logger,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordService passwordService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Login utente con email e password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto loginRequest)
    {
        try
        {
            // Trova utente per email
            var user = await _unitOfWork.Repository<User>()
                .GetFirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginRequest.Email);
                return Unauthorized(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Email o password non validi"
                });
            }

            // Verifica se l'utente può accedere (per ora solo IsActive e soft delete)
            if (!user.IsActive || user.IsDeleted)
            {
                _logger.LogWarning("Login attempt for blocked/inactive user: {UserId}", user.Id);

                var blockMessage = !user.IsActive ? "Account non attivo" : "Account eliminato";

                return Unauthorized(new AuthConfirmationDto
                {
                    Success = false,
                    Message = blockMessage
                });
            }

            // Verifica se temporaneamente bloccato (per ora skip questa logica)
            // TODO: Implementare logica blocco temporaneo quando avremo i nuovi campi

            // Verifica password
            var isPasswordValid = _passwordService.VerifyPassword(
                loginRequest.Password,
                user.PasswordHash,
                user.PasswordSalt);

            if (!isPasswordValid)
            {
                // Per ora non registriamo tentativi falliti - TODO: implementare con nuovi campi
                _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);
                return Unauthorized(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Email o password non validi"
                });
            }

            // Verifica se deve cambiare password (per ora sempre false)
            if (user.MustChangePassword)
            {
                _logger.LogInformation("User {UserId} must change password", user.Id);
                return Ok(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Password scaduta. È necessario cambiarla.",
                    Data = new Dictionary<string, object>
                    {
                        ["mustChangePassword"] = true,
                        ["userId"] = user.Id
                    }
                });
            }

            // Login riuscito - aggiorna lastLogin manualmente
            user.LastLoginAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            // Genera JWT token
            var tokenResult = await _jwtService.GenerateTokenAsync(user, loginRequest.RememberMe);

            // Crea risposta
            var response = new AuthResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                TokenType = tokenResult.TokenType,
                User = MapToUserInfoDto(user),
                Session = new SessionInfoDto
                {
                    SessionId = tokenResult.SessionId,
                    StartedAt = DateTime.UtcNow,
                    ExpiresAt = tokenResult.ExpiresAt,
                    IPAddress = GetClientIPAddress(),
                    DeviceType = GetDeviceType(loginRequest.DeviceInfo),
                    IsNewDevice = true, // TODO: Implementare logica rilevamento nuovo device
                    IsNewLocation = true // TODO: Implementare logica rilevamento nuova location
                }
            };

            _logger.LogInformation("Successful login for user {UserId}", user.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", loginRequest.Email);
            return StatusCode(500, new AuthConfirmationDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Registrazione nuovo utente
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthConfirmationDto>> Register(RegisterRequestDto registerRequest)
    {
        try
        {
            // Verifica se email già esiste
            var existingUser = await _unitOfWork.Repository<User>()
                .GetFirstOrDefaultAsync(u => u.Email == registerRequest.Email);

            if (existingUser != null)
            {
                return BadRequest(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Email già registrata"
                });
            }

            // Verifica se clinica esiste
            var clinic = await _unitOfWork.Repository<Clinic>()
                .GetByIdAsync(registerRequest.ClinicId);

            if (clinic == null)
            {
                return BadRequest(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Clinica non trovata"
                });
            }

            // Valida password
            var passwordValidation = _passwordService.ValidatePassword(registerRequest.Password);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Password non valida: " + string.Join(", ", passwordValidation.Errors)
                });
            }

            // Hash password
            var (passwordHash, salt) = _passwordService.HashPassword(registerRequest.Password);

            // Crea nuovo utente
            var user = new User
            {
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Email = registerRequest.Email,
                Phone = registerRequest.Phone ?? string.Empty,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                PasswordChangedAt = DateTime.UtcNow,
                Role = registerRequest.Role,
                ClinicId = registerRequest.ClinicId,
                TimeZone = registerRequest.TimeZone,
                PreferredLanguage = registerRequest.PreferredLanguage,
                IsActive = true,
                EmailVerified = false, // TODO: Implementare verifica email
                MustChangePassword = false
            };

            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("New user registered: {UserId} with email {Email}", user.Id, user.Email);

            return Ok(new AuthConfirmationDto
            {
                Success = true,
                Message = "Registrazione completata con successo",
                Data = new Dictionary<string, object>
                {
                    ["userId"] = user.Id,
                    ["email"] = user.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email {Email}", registerRequest.Email);
            return StatusCode(500, new AuthConfirmationDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Refresh token JWT
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenRequestDto refreshRequest)
    {
        try
        {
            var tokenResult = await _jwtService.RefreshTokenAsync(refreshRequest.RefreshToken);

            if (tokenResult == null)
            {
                return Unauthorized(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Refresh token non valido o scaduto"
                });
            }

            // Ottieni informazioni utente per la risposta
            var userId = _jwtService.GetUserIdFromToken(tokenResult.AccessToken);
            if (userId == null)
            {
                return Unauthorized(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Token non valido"
                });
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
            if (user == null)
            {
                return Unauthorized(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Utente non trovato"
                });
            }

            var response = new AuthResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                TokenType = tokenResult.TokenType,
                User = MapToUserInfoDto(user),
                Session = new SessionInfoDto
                {
                    SessionId = tokenResult.SessionId,
                    StartedAt = DateTime.UtcNow,
                    ExpiresAt = tokenResult.ExpiresAt,
                    IPAddress = GetClientIPAddress()
                }
            };

            _logger.LogInformation("Token refreshed for user {UserId}", userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new AuthConfirmationDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Logout utente (revoca token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<AuthConfirmationDto>> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Ottieni token corrente dall'header
            var token = GetTokenFromHeader();
            if (!string.IsNullOrEmpty(token))
            {
                await _jwtService.RevokeTokenAsync(ComputeHash(token), "User logout");
            }

            _logger.LogInformation("User {UserId} logged out", userId);
            return Ok(new AuthConfirmationDto
            {
                Success = true,
                Message = "Logout effettuato con successo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new AuthConfirmationDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Revoca tutte le sessioni attive dell'utente
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<ActionResult<AuthConfirmationDto>> LogoutAll()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            await _jwtService.RevokeUserTokensAsync(userId.Value, "Logout all sessions");

            _logger.LogInformation("All sessions revoked for user {UserId}", userId);
            return Ok(new AuthConfirmationDto
            {
                Success = true,
                Message = "Tutte le sessioni sono state terminate"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout all for user");
            return StatusCode(500, new AuthConfirmationDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Cambio password utente autenticato
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthConfirmationDto>> ChangePassword(ChangePasswordRequestDto changePasswordRequest)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Utente non trovato"
                });
            }

            // Verifica password corrente
            var isCurrentPasswordValid = _passwordService.VerifyPassword(
                changePasswordRequest.CurrentPassword,
                user.PasswordHash,
                user.PasswordSalt);

            if (!isCurrentPasswordValid)
            {
                return BadRequest(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Password corrente non valida"
                });
            }

            // Valida nuova password
            var passwordValidation = _passwordService.ValidatePassword(changePasswordRequest.NewPassword);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new AuthConfirmationDto
                {
                    Success = false,
                    Message = "Nuova password non valida: " + string.Join(", ", passwordValidation.Errors)
                });
            }

            // Aggiorna password manualmente per ora
            var (newPasswordHash, newSalt) = _passwordService.HashPassword(changePasswordRequest.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.PasswordSalt = newSalt;
            user.PasswordChangedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            // Revoca tutte le altre sessioni per sicurezza
            await _jwtService.RevokeUserTokensAsync(userId.Value, "Password changed");

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return Ok(new AuthConfirmationDto
            {
                Success = true,
                Message = "Password cambiata con successo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new AuthConfirmationDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Ottieni informazioni utente corrente
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(MapToUserInfoDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user info");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottieni sessioni attive dell'utente
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ActiveSessionDto>>> GetActiveSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var sessions = await _jwtService.GetActiveSessionsAsync(userId.Value);
            var currentToken = GetTokenFromHeader();
            var currentTokenHash = !string.IsNullOrEmpty(currentToken) ? ComputeHash(currentToken) : null;

            var sessionDtos = sessions.Select(s => new ActiveSessionDto
            {
                SessionId = s.Id,
                StartedAt = s.StartedAt,
                LastUsedAt = s.LastUsedAt,
                ExpiresAt = s.ExpiresAt,
                IsActive = s.IsActive,
                DeviceType = s.DeviceType,
                DeviceName = s.DeviceName,
                Browser = s.Browser,
                OperatingSystem = s.OperatingSystem,
                IPAddress = s.IPAddress,
                Location = s.City,
                IsCurrent = s.TokenHash == currentTokenHash,
                IsSuspicious = s.IsSuspicious,
                SuspiciousReason = s.SuspiciousReason,
                RequestCount = s.RequestCount
            });

            return Ok(sessionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            return StatusCode(500, "Errore interno del server");
        }
    }

    #region Helper Methods

    private UserInfoDto MapToUserInfoDto(User user)
    {
        return new UserInfoDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            ClinicId = user.ClinicId,
            PrimaryClinicId = user.PrimaryClinicId,
            TimeZone = user.TimeZone,
            PreferredLanguage = user.PreferredLanguage,
            LastLoginAt = user.LastLoginAt,
            MustChangePassword = user.MustChangePassword,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string GetClientIPAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetUserAgent()
    {
        return HttpContext.Request.Headers.UserAgent.ToString();
    }

    private string? GetDeviceType(string? deviceInfo)
    {
        if (string.IsNullOrEmpty(deviceInfo)) return null;

        // Logica semplificata per rilevare tipo device
        var userAgent = deviceInfo.ToLower();
        if (userAgent.Contains("mobile")) return "Mobile";
        if (userAgent.Contains("tablet")) return "Tablet";
        return "Desktop";
    }

    private string? GetTokenFromHeader()
    {
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}