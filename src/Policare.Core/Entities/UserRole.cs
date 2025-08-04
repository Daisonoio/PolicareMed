namespace PoliCare.Core.Entities;

public enum UserRole
{
    SuperAdmin = 0,
    ClinicAdmin = 1,
    Doctor = 2,
    Nurse = 3,
    Receptionist = 4,
    Patient = 5
}

public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5,
    Rescheduled = 6
}

public enum AppointmentType
{
    FirstVisit = 0,
    FollowUp = 1,
    Emergency = 2,
    Surgery = 3,
    Consultation = 4,
    Diagnostic = 5,
    Therapy = 6
}

public enum MedicalRecordType
{
    Visit = 0,
    Diagnosis = 1,
    Prescription = 2,
    LabResults = 3,
    Imaging = 4,
    Surgery = 5,
    Notes = 6,
    Referral = 7
}