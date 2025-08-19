namespace PoliCare.Core.Entities;

/// <summary>
/// Stati possibili di un appuntamento durante il suo ciclo di vita
/// </summary>
public enum AppointmentStatus
{
    /// <summary>
    /// Appuntamento programmato - Stato iniziale
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Appuntamento confermato dal paziente
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Paziente è arrivato e in attesa
    /// </summary>
    CheckedIn = 2,

    /// <summary>
    /// Appuntamento in corso
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Appuntamento completato con successo
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Appuntamento cancellato (generico per compatibilità)
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Appuntamento cancellato dal paziente
    /// </summary>
    CancelledByPatient = 6,

    /// <summary>
    /// Appuntamento cancellato dalla clinica
    /// </summary>
    CancelledByClinic = 7,

    /// <summary>
    /// Paziente non si è presentato
    /// </summary>
    NoShow = 8,

    /// <summary>
    /// Appuntamento riprogrammato
    /// </summary>
    Rescheduled = 9,

    /// <summary>
    /// In attesa di conferma
    /// </summary>
    PendingConfirmation = 10,

    /// <summary>
    /// Appuntamento scaduto/non utilizzato
    /// </summary>
    Expired = 11
}