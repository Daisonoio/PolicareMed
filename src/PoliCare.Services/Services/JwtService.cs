using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces;

namespace PoliCare.Services.Services;

/// <summary>
/// Servizio per gestione JWT Token e sessioni utente
/// Implementa generazione, validazione e gestione avanzata dei token
/// </summary>
public class JwtService : IJwtService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(
        IUnitOfWork unitOfWork,
        ILogger<JwtService> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Genera JWT token per utente autenticato
    /// </summary>
    public async Task<JwtTokenResult> GenerateTokenAsync(User user, bool rememberMe = false)
    {
        try
        {
            // Determina durata token
            var expiryMinutes = rememberMe ?
                _jwtSettings.RememberMeExpiryDays * 24 * 60 :
                _jwtSettings.ExpiryMinutes;

            var expiresAt = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(expiryMinutes), DateTimeKind.Utc);

            // Crea claims per il token
            var claims = CreateClaims(user);

            // Genera JWT token
            var accessToken = GenerateJwtToken(claims, expiresAt);

            // Genera refresh token se abilitato
            var refreshToken = _jwtSettings.EnableRefreshToken ? GenerateRefreshToken() : string.Empty;

            // Salva sessione nel database
            var sessionId = await CreateUserSessionAsync(user, accessToken, refreshToken, expiresAt);

            _logger.LogInformation("Token generated for user {UserId}, session {SessionId}", user.Id, sessionId);

            return new JwtTokenResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                SessionId = sessionId,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for user {UserId}", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Genera refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Valida e decodifica JWT token
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Estrae User ID dal token JWT
    /// </summary>
    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                             principal?.FindFirst("userId")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting user ID from token");
            return null;
        }
    }

    /// <summary>
    /// Estrae claims dal token JWT
    /// </summary>
    public IEnumerable<Claim> GetClaimsFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            return principal?.Claims ?? Enumerable.Empty<Claim>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting claims from token");
            return Enumerable.Empty<Claim>();
        }
    }

    /// <summary>
    /// Verifica se il token è scaduto
    /// </summary>
    public bool IsTokenExpired(string token)
    {
        try
        {
            var jsonToken = _tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking token expiration");
            return true; // Considera scaduto se non può essere letto
        }
    }

    /// <summary>
    /// Rinnova token utilizzando refresh token
    /// </summary>
    public async Task<JwtTokenResult?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // Hash del refresh token per ricerca sicura
            var refreshTokenHash = ComputeHash(refreshToken);

            // Trova sessione con questo refresh token
            var session = await _unitOfWork.Repository<UserSession>()
                .GetFirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash &&
                                           s.IsActive &&
                                           !s.IsRevoked &&
                                           s.ExpiresAt > DateTime.UtcNow);

            if (session == null)
            {
                _logger.LogWarning("Refresh token not found or expired");
                return null;
            }

            // Carica utente associato
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(session.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User not found or cannot login for session {SessionId}", session.Id);
                await RevokeTokenAsync(refreshTokenHash, "User not valid");
                return null;
            }

            // Revoca sessione corrente
            session.IsRevoked = true;
            session.IsActive = false;
            session.RevokedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            session.RevokeReason = "Token refreshed";
            _unitOfWork.Repository<UserSession>().Update(session);

            // Genera nuovo token
            var newTokenResult = await GenerateTokenAsync(user, false);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Token refreshed for user {UserId}", user.Id);
            return newTokenResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return null;
        }
    }

    /// <summary>
    /// Revoca tutti i token di un utente
    /// </summary>
    public async Task RevokeUserTokensAsync(Guid userId, string reason = "Manual revocation")
    {
        try
        {
            var sessions = await _unitOfWork.Repository<UserSession>()
                .GetWhereAsync(s => s.UserId == userId && s.IsActive && !s.IsRevoked);

            foreach (var session in sessions)
            {
                session.IsRevoked = true;
                session.IsActive = false;
                session.RevokedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                session.RevokeReason = reason;
                _unitOfWork.Repository<UserSession>().Update(session);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Revoked {Count} sessions for user {UserId}. Reason: {Reason}",
                sessions.Count(), userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking user tokens for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Revoca un token specifico
    /// </summary>
    public async Task RevokeTokenAsync(string tokenHash, string reason = "Manual revocation")
    {
        try
        {
            var session = await _unitOfWork.Repository<UserSession>()
                .GetFirstOrDefaultAsync(s => s.TokenHash == tokenHash || s.RefreshTokenHash == tokenHash);

            if (session != null)
            {
                session.IsRevoked = true;
                session.IsActive = false;
                session.RevokedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                session.RevokeReason = reason;
                _unitOfWork.Repository<UserSession>().Update(session);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Token revoked for session {SessionId}. Reason: {Reason}",
                    session.Id, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            throw;
        }
    }

    /// <summary>
    /// Ottieni sessioni attive per un utente
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId)
    {
        try
        {
            var sessions = await _unitOfWork.Repository<UserSession>()
                .GetWhereAsync(s => s.UserId == userId && s.IsActive && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);

            return sessions.OrderByDescending(s => s.LastUsedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Cleanup delle sessioni scadute
    /// </summary>
    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _unitOfWork.Repository<UserSession>()
                .GetWhereAsync(s => s.ExpiresAt < DateTime.UtcNow.AddDays(-7)); // Grace period 7 giorni

            foreach (var session in expiredSessions)
            {
                await _unitOfWork.Repository<UserSession>().DeleteAsync(session.Id);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// Crea claims per JWT token
    /// </summary>
    private List<Claim> CreateClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("userId", user.Id.ToString()),
            new("email", user.Email),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("role", user.Role.ToString()),
            new("timeZone", user.TimeZone ?? "UTC"),
            new("language", user.PreferredLanguage ?? "en-US"),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Aggiungi claim per primary clinic se presente
        if (user.PrimaryClinicId.HasValue)
        {
            claims.Add(new Claim("primaryClinicId", user.PrimaryClinicId.Value.ToString()));
        }

        return claims;
    }

    /// <summary>
    /// Genera JWT token dalle claims
    /// </summary>
    private string GenerateJwtToken(List<Claim> claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Crea sessione utente nel database
    /// </summary>
    private async Task<Guid> CreateUserSessionAsync(User user, string accessToken, string refreshToken, DateTime expiresAt)
    {
        var tokenHash = ComputeHash(accessToken);
        var refreshTokenHash = !string.IsNullOrEmpty(refreshToken) ? ComputeHash(refreshToken) : null;

        var session = new UserSession
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            RefreshTokenHash = refreshTokenHash,
            StartedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            LastUsedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            ExpiresAt = expiresAt,
            IsActive = true,
            IsRevoked = false,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        await _unitOfWork.Repository<UserSession>().AddAsync(session);
        await _unitOfWork.CompleteAsync();

        return session.Id;
    }

    /// <summary>
    /// Calcola hash SHA256 di una stringa
    /// </summary>
    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}