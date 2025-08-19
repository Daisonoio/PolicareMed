using System.ComponentModel.DataAnnotations;

namespace PoliCare.Core.Entities;

/// <summary>
/// Configurazioni di sicurezza e blocco a livello clinica
/// Gestisce subscription, pagamenti, restrizioni e policies
/// </summary>
public class ClinicSecurity : BaseEntity
{
    /// <summary>
    /// ID della clinica associata
    /// </summary>
    [Required]
    public Guid ClinicId { get; set; }

    // STATO OPERATIVO
    /// <summary>
    /// Clinica attiva e operativa
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Clinica sospesa (non può operare)
    /// </summary>
    public bool IsSuspended { get; set; } = false;

    /// <summary>
    /// Motivo della sospensione
    /// </summary>
    [MaxLength(500)]
    public string? SuspensionReason { get; set; }

    /// <summary>
    /// Utente che ha sospeso la clinica
    /// </summary>
    public Guid? SuspendedBy { get; set; }

    /// <summary>
    /// Timestamp sospensione
    /// </summary>
    public DateTime? SuspendedAt { get; set; }

    /// <summary>
    /// Scadenza sospensione
    /// </summary>
    public DateTime? SuspensionExpiresAt { get; set; }

    // SUBSCRIPTION E PAGAMENTI
    /// <summary>
    /// Piano di abbonamento corrente
    /// </summary>
    [MaxLength(50)]
    public string SubscriptionPlan { get; set; } = "Basic";

    /// <summary>
    /// Stato dell'abbonamento
    /// </summary>
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trial;

    /// <summary>
    /// Data inizio abbonamento
    /// </summary>
    public DateTime? SubscriptionStartDate { get; set; }

    /// <summary>
    /// Data scadenza abbonamento
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }

    /// <summary>
    /// Periodo di grazia dopo scadenza (giorni)
    /// </summary>
    public int GracePeriodDays { get; set; } = 7;

    /// <summary>
    /// Accesso bloccato per mancato pagamento
    /// </summary>
    public bool PaymentBlocked { get; set; } = false;

    /// <summary>
    /// Data blocco pagamento
    /// </summary>
    public DateTime? PaymentBlockedAt { get; set; }

    /// <summary>
    /// Ultimo pagamento ricevuto
    /// </summary>
    public DateTime? LastPaymentDate { get; set; }

    /// <summary>
    /// Importo ultimo pagamento
    /// </summary>
    public decimal? LastPaymentAmount { get; set; }

    // LIMITI E QUOTE
    /// <summary>
    /// Numero massimo utenti consentiti
    /// </summary>
    public int MaxUsers { get; set; } = 5;

    /// <summary>
    /// Numero massimo pazienti consentiti
    /// </summary>
    public int MaxPatients { get; set; } = 1000;

    /// <summary>
    /// Numero massimo appuntamenti al mese
    /// </summary>
    public int MaxAppointmentsPerMonth { get; set; } = 500;

    /// <summary>
    /// Storage massimo consentito (MB)
    /// </summary>
    public int MaxStorageMB { get; set; } = 1000;

    /// <summary>
    /// Utilizzo storage corrente (MB)
    /// </summary>
    public int CurrentStorageUsageMB { get; set; } = 0;

    // RESTRIZIONI ACCESSO
    /// <summary>
    /// Restrizioni IP attive
    /// </summary>
    public bool IPRestrictionsEnabled { get; set; } = false;

    /// <summary>
    /// Lista IP consentiti (JSON array)
    /// </summary>
    public string? AllowedIPAddresses { get; set; }

    /// <summary>
    /// Lista IP bloccati (JSON array)
    /// </summary>
    public string? BlockedIPAddresses { get; set; }

    /// <summary>
    /// Restrizioni geografiche attive
    /// </summary>
    public bool GeoRestrictionsEnabled { get; set; } = false;

    /// <summary>
    /// Paesi consentiti (JSON array)
    /// </summary>
    public string? AllowedCountries { get; set; }

    /// <summary>
    /// Orari di accesso consentiti
    /// </summary>
    public bool AccessHoursRestricted { get; set; } = false;

    /// <summary>
    /// Ora inizio accesso (formato HH:mm)
    /// </summary>
    [MaxLength(5)]
    public string? AccessStartTime { get; set; }

    /// <summary>
    /// Ora fine accesso (formato HH:mm)
    /// </summary>
    [MaxLength(5)]
    public string? AccessEndTime { get; set; }

    /// <summary>
    /// Giorni della settimana consentiti (JSON array)
    /// </summary>
    public string? AllowedDaysOfWeek { get; set; }

    // POLICIES DI SICUREZZA
    /// <summary>
    /// Politica password - Minimo caratteri
    /// </summary>
    public int PasswordMinLength { get; set; } = 8;

    /// <summary>
    /// Richiede caratteri maiuscoli
    /// </summary>
    public bool PasswordRequireUppercase { get; set; } = true;

    /// <summary>
    /// Richiede caratteri minuscoli
    /// </summary>
    public bool PasswordRequireLowercase { get; set; } = true;

    /// <summary>
    /// Richiede numeri
    /// </summary>
    public bool PasswordRequireDigits { get; set; } = true;

    /// <summary>
    /// Richiede caratteri speciali
    /// </summary>
    public bool PasswordRequireSpecialChars { get; set; } = true;

    /// <summary>
    /// Giorni di validità password
    /// </summary>
    public int PasswordExpiryDays { get; set; } = 90;

    /// <summary>
    /// Numero max tentativi login
    /// </summary>
    public int MaxLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Minuti di blocco dopo tentativi falliti
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Durata sessione in ore
    /// </summary>
    public int SessionDurationHours { get; set; } = 8;

    /// <summary>
    /// Inattività max prima logout (minuti)
    /// </summary>
    public int MaxInactivityMinutes { get; set; } = 60;

    // AUDIT E LOGGING
    /// <summary>
    /// Audit logging abilitato
    /// </summary>
    public bool AuditLoggingEnabled { get; set; } = true;

    /// <summary>
    /// Giorni di retention log
    /// </summary>
    public int LogRetentionDays { get; set; } = 365;

    /// <summary>
    /// Notifiche sicurezza abilitate
    /// </summary>
    public bool SecurityNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Email per notifiche sicurezza
    /// </summary>
    [MaxLength(200)]
    public string? SecurityNotificationEmail { get; set; }

    // BACKUP E DISASTER RECOVERY
    /// <summary>
    /// Backup automatico abilitato
    /// </summary>
    public bool AutoBackupEnabled { get; set; } = true;

    /// <summary>
    /// Frequenza backup (giorni)
    /// </summary>
    public int BackupFrequencyDays { get; set; } = 1;

    /// <summary>
    /// Giorni di retention backup
    /// </summary>
    public int BackupRetentionDays { get; set; } = 30;

    /// <summary>
    /// Ultimo backup eseguito
    /// </summary>
    public DateTime? LastBackupDate { get; set; }

    // NAVIGATION PROPERTIES
    public virtual Clinic Clinic { get; set; } = null!;

    // COMPUTED PROPERTIES
    /// <summary>
    /// Verifica se la clinica può operare
    /// </summary>
    public bool CanOperate => IsActive && !IsSuspended && !PaymentBlocked && !IsSubscriptionExpired;

    /// <summary>
    /// Verifica se l'abbonamento è scaduto
    /// </summary>
    public bool IsSubscriptionExpired
    {
        get
        {
            if (SubscriptionEndDate == null) return false;
            var gracePeriodEnd = SubscriptionEndDate.Value.AddDays(GracePeriodDays);
            return DateTime.UtcNow > gracePeriodEnd;
        }
    }

    /// <summary>
    /// Verifica se è nel periodo di grazia
    /// </summary>
    public bool IsInGracePeriod
    {
        get
        {
            if (SubscriptionEndDate == null) return false;
            var now = DateTime.UtcNow;
            var gracePeriodEnd = SubscriptionEndDate.Value.AddDays(GracePeriodDays);
            return now > SubscriptionEndDate && now <= gracePeriodEnd;
        }
    }

    /// <summary>
    /// Giorni rimanenti nel periodo di grazia
    /// </summary>
    public int GracePeriodDaysRemaining
    {
        get
        {
            if (!IsInGracePeriod) return 0;
            var gracePeriodEnd = SubscriptionEndDate!.Value.AddDays(GracePeriodDays);
            return Math.Max(0, (gracePeriodEnd - DateTime.UtcNow).Days);
        }
    }

    /// <summary>
    /// Verifica se ha raggiunto il limite utenti
    /// </summary>
    public bool IsUserLimitReached(int currentUserCount)
    {
        return currentUserCount >= MaxUsers;
    }

    /// <summary>
    /// Verifica se ha raggiunto il limite pazienti
    /// </summary>
    public bool IsPatientLimitReached(int currentPatientCount)
    {
        return currentPatientCount >= MaxPatients;
    }

    /// <summary>
    /// Verifica se ha raggiunto il limite storage
    /// </summary>
    public bool IsStorageLimitReached => CurrentStorageUsageMB >= MaxStorageMB;

    /// <summary>
    /// Percentuale utilizzo storage
    /// </summary>
    public decimal StorageUsagePercentage => MaxStorageMB > 0 ? (decimal)CurrentStorageUsageMB / MaxStorageMB * 100 : 0;

    // METODI HELPER
    /// <summary>
    /// Sospende la clinica
    /// </summary>
    public void Suspend(string reason, Guid suspendedBy, DateTime? expiresAt = null)
    {
        IsSuspended = true;
        SuspensionReason = reason;
        SuspendedBy = suspendedBy;
        SuspendedAt = DateTime.UtcNow;
        SuspensionExpiresAt = expiresAt;
    }

    /// <summary>
    /// Rimuove la sospensione
    /// </summary>
    public void Unsuspend()
    {
        IsSuspended = false;
        SuspensionReason = null;
        SuspendedBy = null;
        SuspendedAt = null;
        SuspensionExpiresAt = null;
    }

    /// <summary>
    /// Blocca per mancato pagamento
    /// </summary>
    public void BlockForPayment()
    {
        PaymentBlocked = true;
        PaymentBlockedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sblocca dopo pagamento
    /// </summary>
    public void UnblockPayment(decimal paymentAmount)
    {
        PaymentBlocked = false;
        PaymentBlockedAt = null;
        LastPaymentDate = DateTime.UtcNow;
        LastPaymentAmount = paymentAmount;
    }

    /// <summary>
    /// Aggiorna utilizzo storage
    /// </summary>
    public void UpdateStorageUsage(int usageMB)
    {
        CurrentStorageUsageMB = usageMB;
    }

    /// <summary>
    /// Verifica se un IP è consentito
    /// </summary>
    public bool IsIPAllowed(string ipAddress)
    {
        if (!IPRestrictionsEnabled) return true;

        // Logica per verificare IP (implementazione semplificata)
        // In produzione usare una libreria per parsing IP ranges
        return true; // TODO: Implementare logica IP
    }

    /// <summary>
    /// Verifica se l'accesso è consentito nell'orario corrente
    /// </summary>
    public bool IsAccessTimeAllowed()
    {
        if (!AccessHoursRestricted) return true;
        if (AccessStartTime == null || AccessEndTime == null) return true;

        var now = DateTime.Now.TimeOfDay;
        var startTime = TimeSpan.Parse(AccessStartTime);
        var endTime = TimeSpan.Parse(AccessEndTime);

        return now >= startTime && now <= endTime;
    }
}

/// <summary>
/// Stati possibili dell'abbonamento
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Periodo di prova
    /// </summary>
    Trial = 0,

    /// <summary>
    /// Attivo e pagato
    /// </summary>
    Active = 1,

    /// <summary>
    /// Scaduto ma in periodo di grazia
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Sospeso per mancato pagamento
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Cancellato definitivamente
    /// </summary>
    Cancelled = 4
}