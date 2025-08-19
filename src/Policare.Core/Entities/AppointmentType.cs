namespace PoliCare.Core.Entities;

/// <summary>
/// Tipologie di appuntamento per classificazione e gestione
/// </summary>
public enum AppointmentType
{
    /// <summary>
    /// Prima visita - Nuova valutazione paziente
    /// </summary>
    FirstVisit = 0,

    /// <summary>
    /// Visita di controllo - Follow-up standard
    /// </summary>
    FollowUp = 1,

    /// <summary>
    /// Visita urgente - Richiede attenzione immediata
    /// </summary>
    Urgent = 2,

    /// <summary>
    /// Consulto specialistico - Valutazione specialistica
    /// </summary>
    Consultation = 3,

    /// <summary>
    /// Procedura diagnostica - Esami, analisi
    /// </summary>
    Diagnostic = 4,

    /// <summary>
    /// Trattamento terapeutico - Interventi, terapie
    /// </summary>
    Treatment = 5,

    /// <summary>
    /// Riabilitazione - Sedute di fisioterapia, riabilitazione
    /// </summary>
    Rehabilitation = 6,

    /// <summary>
    /// Check-up preventivo - Controlli di routine
    /// </summary>
    Preventive = 7,

    /// <summary>
    /// Teleconsulto - Visita online/remota
    /// </summary>
    Telemedicine = 8
}