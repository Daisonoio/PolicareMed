namespace PoliCare.Services.Interfaces;

/// <summary>
/// Interface per servizio gestione password sicure
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Genera hash sicuro della password con salt
    /// </summary>
    (string passwordHash, string salt) HashPassword(string password);

    /// <summary>
    /// Verifica password contro hash
    /// </summary>
    bool VerifyPassword(string password, string hash, string salt);

    /// <summary>
    /// Genera salt casuale
    /// </summary>
    string GenerateSalt();

    /// <summary>
    /// Valida robustezza password
    /// </summary>
    PasswordValidationResult ValidatePassword(string password, PasswordPolicy? policy = null);

    /// <summary>
    /// Genera password temporanea sicura
    /// </summary>
    string GenerateTemporaryPassword(int length = 12);

    /// <summary>
    /// Calcola forza password (0-100)
    /// </summary>
    int CalculatePasswordStrength(string password);
}

/// <summary>
/// Policy per validazione password
/// </summary>
public class PasswordPolicy
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigits { get; set; } = true;
    public bool RequireSpecialChars { get; set; } = true;
    public bool ForbidCommonPatterns { get; set; } = true;

    public static PasswordPolicy Default => new();

    public static PasswordPolicy Strict => new()
    {
        MinLength = 12,
        RequireUppercase = true,
        RequireLowercase = true,
        RequireDigits = true,
        RequireSpecialChars = true,
        ForbidCommonPatterns = true
    };

    public static PasswordPolicy Relaxed => new()
    {
        MinLength = 6,
        RequireUppercase = false,
        RequireLowercase = true,
        RequireDigits = true,
        RequireSpecialChars = false,
        ForbidCommonPatterns = false
    };
}

/// <summary>
/// Risultato validazione password
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; } = new();
    public int Strength { get; set; }

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public string GetStrengthText()
    {
        return Strength switch
        {
            < 20 => "Molto debole",
            < 40 => "Debole",
            < 60 => "Media",
            < 80 => "Forte",
            _ => "Molto forte"
        };
    }
}