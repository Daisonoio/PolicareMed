using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

/// <summary>
/// Entità per gestione sessioni utente avanzata
/// Traccia accessi attivi, dispositivi, posizioni e sicurezza
/// </summary>
public class UserSession : BaseEntity
{
    /// <summary>
    /// ID dell'utente proprietario della sessione
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Token di sessione JWT (hash per sicurezza)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token (hash per sicurezza)
    /// </summary>
    [MaxLength(64)]
    public string? RefreshTokenHash { get; set; }

    // INFORMAZIONI SESSIONE
    /// <summary>
    /// Timestamp inizio sessione
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ultimo utilizzo della sessione
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Scadenza sessione
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Sessione attiva
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sessione revocata manualmente
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Timestamp revoca
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Motivo della revoca
    /// </summary>
    [MaxLength(255)]
    public string? RevokeReason { get; set; }

    // INFORMAZIONI DEVICE/BROWSER
    /// <summary>
    /// Indirizzo IP del client
    /// </summary>
    [MaxLength(45)] // IPv6 support
    public string? IPAddress { get; set; }

    /// <summary>
    /// User agent del browser/app
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Nome del dispositivo identificato
    /// </summary>
    [MaxLength(100)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Tipo di dispositivo (Desktop, Mobile, Tablet)
    /// </summary>
    [MaxLength(50)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Sistema operativo
    /// </summary>
    [MaxLength(100)]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Browser utilizzato
    /// </summary>
    [MaxLength(100)]
    public string? Browser { get; set; }

    // GEOLOCALIZZAZIONE
    /// <summary>
    /// Paese di accesso
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// Regione/Stato di accesso
    /// </summary>
    [MaxLength(100)]
    public string? Region { get; set; }

    /// <summary>
    /// Città di accesso
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Latitudine (per analisi geografiche)
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitudine (per analisi geografiche)
    /// </summary>
    public decimal? Longitude { get; set; }

    // SICUREZZA E TRACKING
    /// <summary>
    /// Sessione contrassegnata come sospetta
    /// </summary>
    public bool IsSuspicious { get; set; } = false;

    /// <summary>
    /// Motivo per cui è sospetta
    /// </summary>
    [MaxLength(255)]
    public string? SuspiciousReason { get; set; }

    /// <summary>
    /// Numero di richieste effettuate in questa sessione
    /// </summary>
    public int RequestCount { get; set; } = 0;

    /// <summary>
    /// Ultima richiesta effettuata
    /// </summary>
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// Endpoint dell'ultima richiesta
    /// </summary>
    [MaxLength(255)]
    public string? LastEndpoint { get; set; }

    // NAVIGATION PROPERTIES
    public virtual User User { get; set; } = null!;

    // COMPUTED PROPERTIES
    /// <summary>
    /// Verifica se la sessione è ancora valida
    /// </summary>
    public bool IsValid => IsActive && !IsRevoked && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Durata della sessione
    /// </summary>
    public TimeSpan Duration => LastUsedAt - StartedAt;

    /// <summary>
    /// Verifica se è una sessione di lunga durata (>24h)
    /// </summary>
    public bool IsLongSession => Duration.TotalHours > 24;

    /// <summary>
    /// Tempo rimanente prima della scadenza
    /// </summary>
    public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;

    /// <summary>
    /// Verifica se la sessione sta per scadere (< 1 ora)
    /// </summary>
    public bool IsNearExpiry => TimeUntilExpiry.TotalHours < 1;

    // METODI HELPER
    /// <summary>
    /// Aggiorna timestamp ultimo utilizzo
    /// </summary>
    public void UpdateLastUsed(string? endpoint = null)
    {
        LastUsedAt = DateTime.UtcNow;
        LastRequestAt = DateTime.UtcNow;
        RequestCount++;

        if (endpoint != null)
            LastEndpoint = endpoint;
    }

    /// <summary>
    /// Revoca la sessione
    /// </summary>
    public void Revoke(string reason = "Manual revocation")
    {
        IsRevoked = true;
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokeReason = reason;
    }

    /// <summary>
    /// Marca come sospetta
    /// </summary>
    public void MarkAsSuspicious(string reason)
    {
        IsSuspicious = true;
        SuspiciousReason = reason;
    }

    /// <summary>
    /// Estende la scadenza della sessione
    /// </summary>
    public void ExtendExpiry(TimeSpan extension)
    {
        ExpiresAt = ExpiresAt.Add(extension);
    }

    /// <summary>
    /// Verifica se la sessione proviene da una nuova posizione
    /// </summary>
    public bool IsFromNewLocation(UserSession? previousSession)
    {
        if (previousSession == null) return false;

        return Country != previousSession.Country ||
               Region != previousSession.Region ||
               City != previousSession.City;
    }

    /// <summary>
    /// Verifica se la sessione proviene da un nuovo dispositivo
    /// </summary>
    public bool IsFromNewDevice(UserSession? previousSession)
    {
        if (previousSession == null) return false;

        return DeviceType != previousSession.DeviceType ||
               OperatingSystem != previousSession.OperatingSystem ||
               Browser != previousSession.Browser;
    }
}