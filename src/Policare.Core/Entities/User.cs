using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

/// <summary>
/// Entità User potenziata per sistema authentication enterprise
/// Include gestione sessioni, blocchi, audit e sicurezza avanzata
/// </summary>
public class User : BaseEntity
{
    // INFORMAZIONI BASE
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    // AUTENTICAZIONE E SICUREZZA
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// Salt utilizzato per l'hashing della password
    /// </summary>
    [Required]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// Ultimo cambio password (per policy scadenza)
    /// </summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>
    /// Flag per forzare cambio password al prossimo login
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    // ASSOCIAZIONI E CONTESTO
    public Guid ClinicId { get; set; }

    /// <summary>
    /// ID della clinica primaria (per utenti multi-clinica)
    /// </summary>
    public Guid? PrimaryClinicId { get; set; }

    // ACCESSO E SESSIONI
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Ultimo IP di accesso
    /// </summary>
    [MaxLength(45)] // IPv6 support
    public string? LastLoginIP { get; set; }

    /// <summary>
    /// User agent dell'ultimo accesso
    /// </summary>
    [MaxLength(500)]
    public string? LastUserAgent { get; set; }

    /// <summary>
    /// Numero tentativi di login falliti consecutivi
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Timestamp ultimo tentativo fallito
    /// </summary>
    public DateTime? LastFailedLoginAt { get; set; }

    // SISTEMA DI BLOCCO
    /// <summary>
    /// Utente bloccato (non può accedere)
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>
    /// Motivo del blocco
    /// </summary>
    [MaxLength(500)]
    public string? BlockReason { get; set; }

    /// <summary>
    /// Utente che ha applicato il blocco
    /// </summary>
    public Guid? BlockedBy { get; set; }

    /// <summary>
    /// Timestamp del blocco
    /// </summary>
    public DateTime? BlockedAt { get; set; }

    /// <summary>
    /// Scadenza del blocco (null = permanente)
    /// </summary>
    public DateTime? BlockExpiresAt { get; set; }

    // CONTROLLO ACCESSO AVANZATO
    /// <summary>
    /// Account attivo (diverso da soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Email verificata
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Token di verifica email
    /// </summary>
    [MaxLength(100)]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Token di reset password
    /// </summary>
    [MaxLength(100)]
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Scadenza token reset password
    /// </summary>
    public DateTime? PasswordResetTokenExpires { get; set; }

    // PREFERENZE E CONFIGURAZIONI
    /// <summary>
    /// Timezone dell'utente
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>
    /// Lingua preferita
    /// </summary>
    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "it-IT";

    /// <summary>
    /// Configurazioni personalizzate (JSON)
    /// </summary>
    public string UserSettings { get; set; } = "{}";

    // MULTI-FACTOR AUTHENTICATION (FUTURO)
    /// <summary>
    /// 2FA abilitato
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Secret per TOTP (Time-based One-Time Password)
    /// </summary>
    [MaxLength(32)]
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Codici di backup 2FA
    /// </summary>
    public string? BackupCodes { get; set; }

    // SUBSCRIPTION E PAGAMENTI
    /// <summary>
    /// Accesso sospeso per mancato pagamento
    /// </summary>
    public bool PaymentSuspended { get; set; } = false;

    /// <summary>
    /// Data sospensione pagamento
    /// </summary>
    public DateTime? PaymentSuspendedAt { get; set; }

    // NAVIGATION PROPERTIES
    public virtual Clinic Clinic { get; set; } = null!;
    public virtual Clinic? PrimaryClinic { get; set; }
    public virtual Doctor? Doctor { get; set; }

    // COMPUTED PROPERTIES
    /// <summary>
    /// Nome completo dell'utente
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Verifica se l'utente può accedere al sistema
    /// </summary>
    public bool CanLogin => IsActive && !IsDeleted && !IsBlocked && !PaymentSuspended;

    /// <summary>
    /// Verifica se l'account è temporaneamente bloccato per troppi tentativi
    /// </summary>
    public bool IsTemporarilyLocked
    {
        get
        {
            if (FailedLoginAttempts < 5) return false;
            if (LastFailedLoginAt == null) return false;

            // Blocco per 30 minuti dopo 5 tentativi falliti
            return DateTime.UtcNow < LastFailedLoginAt.Value.AddMinutes(30);
        }
    }

    /// <summary>
    /// Verifica se la password deve essere cambiata
    /// </summary>
    public bool PasswordExpired
    {
        get
        {
            if (MustChangePassword) return true;
            if (PasswordChangedAt == null) return true;

            // Password scade dopo 90 giorni per admin, 180 per altri
            var maxDays = Role.IsAdmin() ? 90 : 180;
            return DateTime.UtcNow > PasswordChangedAt.Value.AddDays(maxDays);
        }
    }

    // METODI HELPER
    /// <summary>
    /// Applica blocco utente
    /// </summary>
    public void Block(string reason, Guid blockedBy, DateTime? expiresAt = null)
    {
        IsBlocked = true;
        BlockReason = reason;
        BlockedBy = blockedBy;
        BlockedAt = DateTime.UtcNow;
        BlockExpiresAt = expiresAt;
    }

    /// <summary>
    /// Rimuove blocco utente
    /// </summary>
    public void Unblock()
    {
        IsBlocked = false;
        BlockReason = null;
        BlockedBy = null;
        BlockedAt = null;
        BlockExpiresAt = null;
    }

    /// <summary>
    /// Registra tentativo di login fallito
    /// </summary>
    public void RegisterFailedLogin(string? ipAddress = null)
    {
        FailedLoginAttempts++;
        LastFailedLoginAt = DateTime.UtcNow;
        if (ipAddress != null) LastLoginIP = ipAddress;
    }

    /// <summary>
    /// Registra login riuscito
    /// </summary>
    public void RegisterSuccessfulLogin(string? ipAddress = null, string? userAgent = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIP = ipAddress;
        LastUserAgent = userAgent;
        FailedLoginAttempts = 0;
        LastFailedLoginAt = null;
    }

    /// <summary>
    /// Forza cambio password
    /// </summary>
    public void ForcePasswordChange()
    {
        MustChangePassword = true;
    }

    /// <summary>
    /// Aggiorna password
    /// </summary>
    public void UpdatePassword(string passwordHash, string salt)
    {
        PasswordHash = passwordHash;
        PasswordSalt = salt;
        PasswordChangedAt = DateTime.UtcNow;
        MustChangePassword = false;
        PasswordResetToken = null;
        PasswordResetTokenExpires = null;
    }
}