namespace PoliCare.Core.Entities;

/// <summary>
/// Gerarchia completa di ruoli per sistema PoliCare
/// Ordinati per livello di privilegio (0 = massimo)
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Super amministratore piattaforma - Accesso totale
    /// Gestisce: Tutte le cliniche, configurazioni globali, prezzi, funzionalità
    /// </summary>
    SuperAdmin = 0,

    /// <summary>
    /// Amministratore piattaforma - Gestione multiple cliniche
    /// Gestisce: Cliniche assegnate, utenti, supporto tecnico
    /// </summary>
    PlatformAdmin = 1,

    /// <summary>
    /// Proprietario clinica - Controllo completo della propria clinica
    /// Gestisce: Configurazioni clinica, utenti, medici, tariffe, statistiche
    /// </summary>
    ClinicOwner = 2,

    /// <summary>
    /// Manager clinica - Gestione operativa
    /// Gestisce: Scheduling, staff, report operativi, configurazioni base
    /// </summary>
    ClinicManager = 3,

    /// <summary>
    /// Responsabile amministrativo - Gestione amministrativa
    /// Gestisce: Fatturazione, pagamenti, report finanziari, pazienti
    /// </summary>
    AdminStaff = 4,

    /// <summary>
    /// Medico/Specialista - Focus clinico
    /// Gestisce: Propri appuntamenti, pazienti, cartelle cliniche, agenda
    /// </summary>
    Doctor = 5,

    /// <summary>
    /// Infermiere - Supporto medico
    /// Gestisce: Assistenza medica, cartelle assegnate, preparazione visite
    /// </summary>
    Nurse = 6,

    /// <summary>
    /// Receptionist - Front office
    /// Gestisce: Appuntamenti, accoglienza, comunicazioni base
    /// </summary>
    Receptionist = 7,

    /// <summary>
    /// Paziente - Accesso limitato al portale personale
    /// Gestisce: Propri appuntamenti, cartella clinica, comunicazioni
    /// </summary>
    Patient = 8
}

/// <summary>
/// Estensioni per UserRole con utilities per gestione privilegi
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Verifica se un ruolo ha privilegi amministrativi
    /// </summary>
    public static bool IsAdmin(this UserRole role)
    {
        return role <= UserRole.AdminStaff;
    }

    /// <summary>
    /// Verifica se un ruolo ha accesso medico
    /// </summary>
    public static bool IsMedicalStaff(this UserRole role)
    {
        return role >= UserRole.Doctor && role <= UserRole.Nurse;
    }

    /// <summary>
    /// Verifica se un ruolo ha accesso staff
    /// </summary>
    public static bool IsStaff(this UserRole role)
    {
        return role <= UserRole.Receptionist;
    }

    /// <summary>
    /// Verifica se un ruolo può gestire utenti
    /// </summary>
    public static bool CanManageUsers(this UserRole role)
    {
        return role <= UserRole.ClinicManager;
    }

    /// <summary>
    /// Verifica se un ruolo può gestire la clinica
    /// </summary>
    public static bool CanManageClinic(this UserRole role)
    {
        return role <= UserRole.ClinicOwner;
    }

    /// <summary>
    /// Verifica se un ruolo può accedere al sistema finanziario
    /// </summary>
    public static bool HasFinancialAccess(this UserRole role)
    {
        return role <= UserRole.AdminStaff;
    }

    /// <summary>
    /// Verifica se un ruolo può gestire appuntamenti
    /// </summary>
    public static bool CanManageAppointments(this UserRole role)
    {
        return role <= UserRole.Receptionist;
    }

    /// <summary>
    /// Verifica se un ruolo può accedere alle cartelle cliniche
    /// </summary>
    public static bool HasMedicalRecordAccess(this UserRole role)
    {
        return role <= UserRole.Nurse;
    }

    /// <summary>
    /// Ottiene la descrizione user-friendly del ruolo
    /// </summary>
    public static string GetDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.SuperAdmin => "Super Amministratore",
            UserRole.PlatformAdmin => "Amministratore Piattaforma",
            UserRole.ClinicOwner => "Proprietario Clinica",
            UserRole.ClinicManager => "Manager Clinica",
            UserRole.AdminStaff => "Staff Amministrativo",
            UserRole.Doctor => "Medico",
            UserRole.Nurse => "Infermiere",
            UserRole.Receptionist => "Receptionist",
            UserRole.Patient => "Paziente",
            _ => role.ToString()
        };
    }

    /// <summary>
    /// Ottiene i ruoli che questo ruolo può gestire
    /// </summary>
    public static IEnumerable<UserRole> GetManageableRoles(this UserRole role)
    {
        return role switch
        {
            UserRole.SuperAdmin => Enum.GetValues<UserRole>(),
            UserRole.PlatformAdmin => Enum.GetValues<UserRole>().Where(r => r >= UserRole.ClinicOwner),
            UserRole.ClinicOwner => Enum.GetValues<UserRole>().Where(r => r >= UserRole.ClinicManager),
            UserRole.ClinicManager => Enum.GetValues<UserRole>().Where(r => r >= UserRole.AdminStaff),
            UserRole.AdminStaff => Enum.GetValues<UserRole>().Where(r => r >= UserRole.Receptionist),
            _ => Enumerable.Empty<UserRole>()
        };
    }

    /// <summary>
    /// Verifica se un ruolo può modificare un altro ruolo
    /// </summary>
    public static bool CanManageRole(this UserRole managerRole, UserRole targetRole)
    {
        return GetManageableRoles(managerRole).Contains(targetRole);
    }

    /// <summary>
    /// Ottiene il livello di priorità del ruolo (0 = più alto)
    /// </summary>
    public static int GetPriorityLevel(this UserRole role)
    {
        return (int)role;
    }
}