using PoliCare.Core.Entities;
using System.Security.Claims;

namespace PoliCare.Services.Interfaces;

/// <summary>
/// Interface per servizio JWT Token management
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera JWT token per utente autenticato
    /// </summary>
    Task<JwtTokenResult> GenerateTokenAsync(User user, bool rememberMe = false);

    /// <summary>
    /// Genera refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida e decodifica JWT token
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Estrae User ID dal token JWT
    /// </summary>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Estrae claims dal token JWT
    /// </summary>
    IEnumerable<Claim> GetClaimsFromToken(string token);

    /// <summary>
    /// Verifica se il token è scaduto
    /// </summary>
    bool IsTokenExpired(string token);

    /// <summary>
    /// Rinnova token utilizzando refresh token
    /// </summary>
    Task<JwtTokenResult?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revoca tutti i token di un utente
    /// </summary>
    Task RevokeUserTokensAsync(Guid userId, string reason = "Manual revocation");

    /// <summary>
    /// Revoca un token specifico
    /// </summary>
    Task RevokeTokenAsync(string tokenHash, string reason = "Manual revocation");

    /// <summary>
    /// Ottieni sessioni attive per un utente
    /// </summary>
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId);

    /// <summary>
    /// Cleanup delle sessioni scadute
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}

/// <summary>
/// Risultato generazione JWT token
/// </summary>
public class JwtTokenResult
{
    /// <summary>
    /// JWT Access Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Data scadenza token
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// ID della sessione creata
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Tipo di token
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Configurazioni JWT
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Chiave segreta per firmare i token
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Issuer del token (nome applicazione)
    /// </summary>
    public string Issuer { get; set; } = "PoliCare";

    /// <summary>
    /// Audience del token
    /// </summary>
    public string Audience { get; set; } = "PoliCare-Users";

    /// <summary>
    /// Durata del token in minuti (default: 60)
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Durata del refresh token in giorni (default: 30)
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 30;

    /// <summary>
    /// Abilita refresh token automatico
    /// </summary>
    public bool EnableRefreshToken { get; set; } = true;

    /// <summary>
    /// Durata estesa per "Remember Me" in giorni
    /// </summary>
    public int RememberMeExpiryDays { get; set; } = 30;
}