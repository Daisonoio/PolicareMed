namespace PoliCare.Core.Entities;

/// <summary>
/// Tipologie di cartelle cliniche e documenti medici
/// </summary>
public enum MedicalRecordType
{
    /// <summary>
    /// Anamnesi - Storia clinica del paziente
    /// </summary>
    Anamnesis = 0,

    /// <summary>
    /// Diagnosi - Valutazione diagnostica
    /// </summary>
    Diagnosis = 1,

    /// <summary>
    /// Piano di trattamento - Strategia terapeutica
    /// </summary>
    TreatmentPlan = 2,

    /// <summary>
    /// Note di visita - Annotazioni durante l'esame
    /// </summary>
    VisitNotes = 3,

    /// <summary>
    /// Prescrizione - Farmaci e terapie prescritte
    /// </summary>
    Prescription = 4,

    /// <summary>
    /// Referto di laboratorio - Risultati analisi
    /// </summary>
    LabReport = 5,

    /// <summary>
    /// Referto imaging - Radiologie, ecografie, etc.
    /// </summary>
    ImagingReport = 6,

    /// <summary>
    /// Consenso informato - Documenti consenso
    /// </summary>
    InformedConsent = 7,

    /// <summary>
    /// Lettera di dimissione - Documenti di dimissione
    /// </summary>
    DischargeReport = 8,

    /// <summary>
    /// Follow-up - Note di controllo successivo
    /// </summary>
    FollowUp = 9,

    /// <summary>
    /// Certificato medico - Attestazioni e certificazioni
    /// </summary>
    MedicalCertificate = 10,

    /// <summary>
    /// Allegato generico - Altri documenti
    /// </summary>
    Attachment = 11
}