using FluentAssertions;
using PoliCare.Core.Entities;
using PoliCare.Tests.Helpers;
using Xunit;

namespace PoliCare.Tests.Unit.Core.Entities;

/// <summary>
/// Test per l'entità User - verifica business logic e validazioni
/// </summary>
public class UserEntityTests
{
    private readonly TestDataBuilder _dataBuilder;

    public UserEntityTests()
    {
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public void User_Creation_Should_Have_Valid_Default_Values()
    {
        // Arrange & Act
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .WithRole(UserRole.Doctor)
            .Build();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.FirstName.Should().Be("Mario");
        user.LastName.Should().Be("Rossi");
        user.Email.Should().Be("mario.rossi@test.com");
        user.Role.Should().Be(UserRole.Doctor);
        user.IsActive.Should().BeTrue();
        user.IsBlocked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.MustChangePassword.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void User_Block_Should_Set_Correct_Properties()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Test", "User")
            .Build();

        var blockReason = "Test suspension";
        var blockedBy = Guid.NewGuid();

        // Act
        user.IsBlocked = true;
        user.BlockReason = blockReason;
        user.BlockedBy = blockedBy;
        user.BlockedAt = DateTime.UtcNow;

        // Assert
        user.IsBlocked.Should().BeTrue();
        user.BlockReason.Should().Be(blockReason);
        user.BlockedBy.Should().Be(blockedBy);
        user.BlockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.PlatformAdmin)]
    [InlineData(UserRole.ClinicOwner)]
    [InlineData(UserRole.ClinicManager)]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.AdminStaff)]
    [InlineData(UserRole.Receptionist)]
    [InlineData(UserRole.Nurse)]
    [InlineData(UserRole.Patient)]
    public void User_Role_Should_Be_Valid_UserRole_Enum(UserRole role)
    {
        // Arrange & Act
        var user = _dataBuilder.CreateUser()
            .WithRole(role)
            .Build();

        // Assert
        user.Role.Should().Be(role);
        Enum.IsDefined(typeof(UserRole), role).Should().BeTrue();
    }

    [Fact]
    public void User_FailedLoginAttempts_Should_Increment_Correctly()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var initialAttempts = user.FailedLoginAttempts;

        // Act
        user.FailedLoginAttempts++;
        user.LastFailedLoginAt = DateTime.UtcNow;

        // Assert
        user.FailedLoginAttempts.Should().Be(initialAttempts + 1);
        user.LastFailedLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void User_Password_Fields_Should_Be_Required()
    {
        // Arrange & Act
        var user = _dataBuilder.CreateUser().Build();

        // Assert
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordSalt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void User_EmailVerification_Should_Work_Correctly()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var verificationToken = Guid.NewGuid().ToString();

        // Act
        user.EmailVerified = false;
        user.EmailVerificationToken = verificationToken;

        // Simulate email verification
        user.EmailVerified = true;
        user.EmailVerificationToken = null;

        // Assert
        user.EmailVerified.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public void User_PasswordReset_Should_Set_Token_And_Expiry()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var resetToken = Guid.NewGuid().ToString();
        var expiryTime = DateTime.UtcNow.AddHours(1);

        // Act
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpires = expiryTime;

        // Assert
        user.PasswordResetToken.Should().Be(resetToken);
        user.PasswordResetTokenExpires.Should().Be(expiryTime);
    }

    [Fact]
    public void User_TwoFactor_Should_Be_Configurable()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var twoFactorSecret = "JBSWY3DPEHPK3PXP";

        // Act
        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = twoFactorSecret;

        // Assert
        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorSecret.Should().Be(twoFactorSecret);
    }

    [Fact]
    public void User_UserSettings_Should_Accept_JSON()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var settings = """{"theme": "dark", "language": "it", "notifications": true}""";

        // Act
        user.UserSettings = settings;

        // Assert
        user.UserSettings.Should().Be(settings);
    }

    [Theory]
    [InlineData("it-IT")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    [InlineData("fr-FR")]
    public void User_PreferredLanguage_Should_Accept_Valid_Locales(string language)
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();

        // Act
        user.PreferredLanguage = language;

        // Assert
        user.PreferredLanguage.Should().Be(language);
    }

    [Theory]
    [InlineData("Europe/Rome")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Tokyo")]
    [InlineData("UTC")]
    public void User_TimeZone_Should_Accept_Valid_TimeZones(string timeZone)
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();

        // Act
        user.TimeZone = timeZone;

        // Assert
        user.TimeZone.Should().Be(timeZone);
    }

    [Fact]
    public void User_Should_Support_Multiple_Clinics()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var primaryClinicId = Guid.NewGuid();
        var currentClinicId = Guid.NewGuid();

        // Act
        user.PrimaryClinicId = primaryClinicId;
        user.ClinicId = currentClinicId;

        // Assert
        user.PrimaryClinicId.Should().Be(primaryClinicId);
        user.ClinicId.Should().Be(currentClinicId);
        user.PrimaryClinicId.Should().NotBe(user.ClinicId);
    }

    [Fact]
    public void User_SoftDelete_Should_Preserve_Data()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var deletedBy = "system";

        // Act
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.DeletedBy.Should().Be(deletedBy);
        // Dati principali dovrebbero rimanere
        user.FirstName.Should().NotBeNullOrEmpty();
        user.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void User_LoginTracking_Should_Update_Correctly()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var loginTime = DateTime.UtcNow;
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        user.LastLoginAt = loginTime;
        user.LastLoginIP = ipAddress;
        user.LastUserAgent = userAgent;
        user.FailedLoginAttempts = 0; // Reset on successful login

        // Assert
        user.LastLoginAt.Should().Be(loginTime);
        user.LastLoginIP.Should().Be(ipAddress);
        user.LastUserAgent.Should().Be(userAgent);
        user.FailedLoginAttempts.Should().Be(0);
    }
}