using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;
using Policare.API.DTOs;
using System.Security.Claims;

namespace Policare.API.Controllers;

/// <summary>
/// Controller per autenticazione e autorizzazione
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Login utente
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        try
        {
            // Trova utente per email
            var user = await _unitOfWork.Repository<User>()
                .GetFirstOrDefaultAsync(u => u.Email == loginRequest.Email.ToLower().Trim() && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginRequest.Email);
                return BadRequest(new { message = "Email o password non validi" });
            }

            // Verifica se l'utente può accedere (usando solo campi esistenti)
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
                return BadRequest(new { message = "Account disattivato" });
            }

            // Verifica password
            var isPasswordValid = _passwordService.VerifyPassword(
                loginRequest.Password,
                user.PasswordHash,
                user.PasswordSalt);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);
                return BadRequest(new { message = "Email o password non validi" });
            }

            // Login riuscito - aggiorna LastLoginAt con UTC esplicito
            user.LastLoginAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            // Genera JWT token
            var tokenResult = await _jwtService.GenerateTokenAsync(user, loginRequest.RememberMe);
            if (tokenResult == null)
            {
                _logger.LogError("Failed to generate token for user: {UserId}", user.Id);
                return StatusCode(500, new { message = "Errore durante la generazione del token" });
            }

            // Verifica se deve cambiare password
            var mustChangePassword = user.MustChangePassword;

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return Ok(new AuthResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    MustChangePassword = mustChangePassword,
                    PrimaryClinicId = user.PrimaryClinicId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", loginRequest.Email);
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Registrazione nuovo utente
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerRequest)
    {
        try
        {
            // Verifica se email già esistente
            var existingUser = await _unitOfWork.Repository<User>()
                .GetFirstOrDefaultAsync(u => u.Email == registerRequest.Email.ToLower().Trim());

            if (existingUser != null)
            {
                return BadRequest(new { message = "Email già registrata" });
            }

            // Verifica se clinica esiste
            var clinic = await _unitOfWork.Repository<Clinic>()
                .GetByIdAsync(registerRequest.ClinicId);

            if (clinic == null)
            {
                return BadRequest(new { message = "Clinica non trovata" });
            }

            // Genera hash password
            var (passwordHash, salt) = _passwordService.HashPassword(registerRequest.Password);
            /*{
              "firstName": "Admin",
              "lastName": "Admin",
              "email": "admin@admin.com",
              "password": "adminadminadmin",
              "confirmPassword": "adminadminadmin",
              "phone": "3391007019",
              "clinicId": "08ddf3af-1037-42ad-82bf-033707c7d124",
              "role": 0,
              "timeZone": "string",
              "preferredLanguage": "string"
            }*/
            // Crea nuovo utente con UTC esplicito
            var user = new User
            {
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Email = registerRequest.Email.ToLower().Trim(),
                Phone = registerRequest.Phone,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                Role = registerRequest.Role,
                IsActive = true,
                MustChangePassword = false,
                ClinicId = (Guid)registerRequest.ClinicId,
                TimeZone = registerRequest.TimeZone ?? "UTC",
                PreferredLanguage = registerRequest.PreferredLanguage ?? "en-US",
                // Usa DateTime.SpecifyKind per garantire UTC
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };

            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.CompleteAsync();

            // Genera JWT token
            var tokenResult = await _jwtService.GenerateTokenAsync(user, false);
            if (tokenResult == null)
            {
                _logger.LogError("Failed to generate token for new user: {UserId}", user.Id);
                return StatusCode(500, new { message = "Errore durante la generazione del token" });
            }

            _logger.LogInformation("New user registered: {UserId}", user.Id);

            return Ok(new AuthResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    MustChangePassword = false,
                    ClinicId = (Guid)user.ClinicId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", registerRequest.Email);
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto refreshRequest)
    {
        try
        {
            var tokenResult = await _jwtService.RefreshTokenAsync(refreshRequest.RefreshToken);

            if (tokenResult == null)
            {
                return Unauthorized(new { message = "Refresh token non valido o scaduto" });
            }

            var userId = _jwtService.GetUserIdFromToken(tokenResult.AccessToken);
            if (userId == null)
            {
                return Unauthorized(new { message = "Token non valido" });
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
            if (user == null)
            {
                return Unauthorized(new { message = "Utente non trovato" });
            }

            return Ok(new AuthResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                TokenType = tokenResult.TokenType,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    MustChangePassword = user.MustChangePassword,
                    PrimaryClinicId = user.PrimaryClinicId,
                    ClinicId = user.ClinicId,
                    TimeZone = user.TimeZone,
                    PreferredLanguage = user.PreferredLanguage,
                    LastLoginAt = user.LastLoginAt,
                    EmailVerified = user.EmailVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled
                },
                Session = new SessionInfoDto
                {
                    SessionId = tokenResult.SessionId,
                    StartedAt = DateTime.UtcNow,
                    ExpiresAt = tokenResult.ExpiresAt,
                    IPAddress = GetClientIPAddress()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Logout utente (revoca token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _jwtService.RevokeUserTokensAsync(userId, "User logout");
                _logger.LogInformation("User {UserId} logged out", userId);
            }

            return Ok(new { message = "Logout effettuato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Cambio password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestDto changePasswordRequest)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new { message = "Token non valido" });
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            // Verifica password corrente
            var isCurrentPasswordValid = _passwordService.VerifyPassword(
                changePasswordRequest.CurrentPassword,
                user.PasswordHash,
                user.PasswordSalt);

            if (!isCurrentPasswordValid)
            {
                return BadRequest(new { message = "Password corrente non valida" });
            }

            // Aggiorna password con UTC esplicito
            var (newPasswordHash, newSalt) = _passwordService.HashPassword(changePasswordRequest.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.PasswordSalt = newSalt;
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Password changed for user: {UserId}", userId);

            return Ok(new { message = "Password modificata con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, new { message = "Errore interno del server" });
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new { message = "Token non valido" });
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            return Ok(new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                PrimaryClinicId = user.PrimaryClinicId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user info");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Ottieni indirizzo IP del client
    /// </summary>
    private string GetClientIPAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Ottieni User Agent del client
    /// </summary>
    private string GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
    }

    #endregion
}