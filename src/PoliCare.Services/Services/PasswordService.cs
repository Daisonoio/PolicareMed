using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PoliCare.Services.Interfaces;

namespace PoliCare.Services.Services;

/// <summary>
/// Servizio per gestione sicura delle password
/// Utilizza PBKDF2 per hashing e algoritmi di validazione avanzati
/// </summary>
public class PasswordService : IPasswordService
{
    private const int SaltSize = 32;
    private const int HashSize = 64;
    private const int Iterations = 10000;

    private readonly ILogger<PasswordService> _logger;

    public PasswordService(ILogger<PasswordService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Crea hash sicuro della password utilizzando PBKDF2
    /// </summary>
    public (string passwordHash, string salt) HashPassword(string password)
    {
        try
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Genera salt casuale
            var salt = GenerateSalt();

            // Genera hash usando PBKDF2
            var hash = GenerateHash(password, salt);

            _logger.LogDebug("Password hashed successfully");
            return (hash, salt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hashing password");
            throw;
        }
    }

    /// <summary>
    /// Verifica password contro hash memorizzato
    /// </summary>
    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
        {
            _logger.LogWarning("Password verification failed: null or empty parameters");
            return false;
        }

        try
        {
            var computedHash = GenerateHash(password, salt);
            var result = SlowEquals(hash, computedHash);

            _logger.LogDebug("Password verification result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    /// <summary>
    /// Genera salt casuale crittograficamente sicuro
    /// </summary>
    public string GenerateSalt()
    {
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[SaltSize];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating salt");
            throw;
        }
    }

    /// <summary>
    /// Valida password secondo policy di sicurezza
    /// </summary>
    public PasswordValidationResult ValidatePassword(string password, PasswordPolicy? policy = null)
    {
        policy ??= PasswordPolicy.Default;
        var result = new PasswordValidationResult();

        try
        {
            if (string.IsNullOrEmpty(password))
            {
                result.AddError("Password è richiesta");
                return result;
            }

            // Verifica lunghezza minima
            if (password.Length < policy.MinLength)
            {
                result.AddError($"Password deve essere almeno {policy.MinLength} caratteri");
            }

            // Verifica lunghezza massima
            if (password.Length > policy.MaxLength)
            {
                result.AddError($"Password non può superare {policy.MaxLength} caratteri");
            }

            // Verifica caratteri maiuscoli
            if (policy.RequireUppercase && !password.Any(char.IsUpper))
            {
                result.AddError("Password deve contenere almeno una lettera maiuscola");
            }

            // Verifica caratteri minuscoli
            if (policy.RequireLowercase && !password.Any(char.IsLower))
            {
                result.AddError("Password deve contenere almeno una lettera minuscola");
            }

            // Verifica numeri
            if (policy.RequireDigits && !password.Any(char.IsDigit))
            {
                result.AddError("Password deve contenere almeno un numero");
            }

            // Verifica caratteri speciali
            if (policy.RequireSpecialChars)
            {
                var specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
                if (!password.Any(specialChars.Contains))
                {
                    result.AddError("Password deve contenere almeno un carattere speciale");
                }
            }

            // Verifica pattern comuni deboli
            if (policy.ForbidCommonPatterns)
            {
                var commonPatterns = new[]
                {
                    @"(.)\1{2,}", // Caratteri ripetuti (aaa, 111)
                    @"(012|123|234|345|456|567|678|789|890)", // Sequenze numeriche
                    @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", // Sequenze alfabetiche
                    @"(qwe|asd|zxc)", // Pattern tastiera
                };

                foreach (var pattern in commonPatterns)
                {
                    if (Regex.IsMatch(password.ToLower(), pattern))
                    {
                        result.AddError("Password contiene pattern comuni non sicuri");
                        break;
                    }
                }
            }

            // Calcola forza password
            result.Strength = CalculatePasswordStrength(password);

            _logger.LogDebug("Password validation completed. Valid: {IsValid}, Strength: {Strength}",
                result.IsValid, result.Strength);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password");
            result.AddError("Errore durante la validazione della password");
            return result;
        }
    }

    /// <summary>
    /// Genera password temporanea sicura
    /// </summary>
    public string GenerateTemporaryPassword(int length = 12)
    {
        try
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "!@#$%^&*";

            var allChars = uppercase + lowercase + digits + specials;
            var random = new Random();
            var password = new StringBuilder();

            // Assicura almeno un carattere di ogni tipo
            password.Append(uppercase[random.Next(uppercase.Length)]);
            password.Append(lowercase[random.Next(lowercase.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(specials[random.Next(specials.Length)]);

            // Riempi il resto casualmente
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Mescola i caratteri
            var result = new string(password.ToString().OrderBy(x => random.Next()).ToArray());

            _logger.LogDebug("Generated temporary password of length {Length}", length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating temporary password");
            throw;
        }
    }

    /// <summary>
    /// Calcola forza password su scala 0-100
    /// </summary>
    public int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        try
        {
            var score = 0;

            // Lunghezza (max 25 punti)
            score += Math.Min(25, password.Length * 2);

            // Varietà caratteri (max 40 punti)
            if (password.Any(char.IsLower)) score += 10;
            if (password.Any(char.IsUpper)) score += 10;
            if (password.Any(char.IsDigit)) score += 10;
            if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c))) score += 10;

            // Complessità (max 20 punti)
            var uniqueChars = password.Distinct().Count();
            score += Math.Min(20, uniqueChars * 2);

            // Lunghezza extra (max 15 punti)
            if (password.Length >= 12) score += 5;
            if (password.Length >= 16) score += 5;
            if (password.Length >= 20) score += 5;

            // Penalità per pattern comuni
            if (Regex.IsMatch(password.ToLower(), @"(.)\1{2,}")) score -= 10; // Caratteri ripetuti
            if (Regex.IsMatch(password.ToLower(), @"(123|abc|qwe)")) score -= 15; // Pattern comuni

            var finalScore = Math.Max(0, Math.Min(100, score));

            _logger.LogDebug("Password strength calculated: {Score}/100", finalScore);
            return finalScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating password strength");
            return 0;
        }
    }

    #region Private Methods

    /// <summary>
    /// Genera hash PBKDF2 della password
    /// </summary>
    private string GenerateHash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(HashSize);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Confronto sicuro per prevenire timing attacks
    /// </summary>
    private static bool SlowEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        var diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }

    #endregion
}