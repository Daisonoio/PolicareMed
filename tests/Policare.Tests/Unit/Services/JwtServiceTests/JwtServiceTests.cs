using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Services;
using PoliCare.Tests.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace PoliCare.Tests.Unit.Services.JwtServiceTests;

/// <summary>
/// Test per JwtService - Gestione token JWT per autenticazione
/// </summary>
public class JwtServiceTests
{
    private readonly TestDataBuilder _dataBuilder;
    private readonly JwtService _jwtService;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtOptions;
    private readonly Mock<ILogger<JwtService>> _mockLogger; // FIX: aggiunta dichiarazione
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;     // FIX: aggiunta dichiarazione


    public JwtServiceTests()
    {
        _dataBuilder = new TestDataBuilder();

        // Setup JWT Settings per test
        var jwtSettings = new JwtSettings
        {
            SecretKey = "TestSecretKeyForPoliCareJwtTokenGenerationInTestEnvironmentOnly123456789",
            Issuer = "PoliCareTest",
            Audience = "PoliCareTestUsers",
            ExpiryInMinutes = 60,
            RefreshTokenExpiryInDays = 7
        };

        _mockJwtOptions = new Mock<IOptions<JwtSettings>>();
        _mockJwtOptions.Setup(x => x.Value).Returns(jwtSettings);

        _mockLogger = new Mock<ILogger<JwtService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _jwtService = new JwtService(_mockUnitOfWork.Object, _mockLogger.Object, _mockJwtOptions.Object);
        _mockLogger = new Mock<ILogger<JwtService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _jwtService = new JwtService(_mockUnitOfWork.Object, _mockLogger.Object, _mockJwtOptions.Object);
    }

    // Sostituisci tutte le chiamate a _jwtService.GenerateToken(user) con _jwtService.GenerateTokenAsync(user).Result

    // Esempio di modifica per il primo test:
    [Fact]
    public async Task GenerateToken_WithValidUser_ShouldReturnValidJWTAsync()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .WithRole(UserRole.Doctor)
            .Build();

        // Act
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Assert
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.CanReadToken(token).Should().BeTrue();

        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.Should().NotBeNull();
        jwtToken.Header.Alg.Should().Be("HS256");
    }

    [Fact]
    public async Task GenerateToken_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .WithRole(UserRole.Doctor)
            .WithClinic(Guid.NewGuid())
            .Build();

        // Act
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();

        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == $"{user.FirstName} {user.LastName}");
        claims.Should().Contain(c => c.Type == "clinic_id" && c.Value == user.ClinicId.ToString());

        jwtToken.Issuer.Should().Be("PoliCareTest");
        jwtToken.Audiences.Should().Contain("PoliCareTestUsers");
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
    public async Task GenerateToken_WithDifferentRoles_ShouldIncludeCorrectRole(UserRole role)
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithRole(role)
            .Build();

        // Act
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(role.ToString());
    }

    [Fact]
    public async Task GenerateToken_ShouldSetCorrectExpiration()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);

        // Act
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var shortExpirySettings = new JwtSettings
        {
            SecretKey = "TestSecretKeyForPoliCareJwtTokenGenerationInTestEnvironmentOnly123456789",
            Issuer = "PoliCareTest",
            Audience = "PoliCareTestUsers",
            ExpiryInMinutes = -1,
            RefreshTokenExpiryInDays = 7
        };

        var mockShortExpiry = new Mock<IOptions<JwtSettings>>();
        mockShortExpiry.Setup(x => x.Value).Returns(shortExpirySettings);

        var shortExpiryService = new JwtService(mockShortExpiry.Object);
        var user = _dataBuilder.CreateUser().Build();
        var expiredToken = (await shortExpiryService.GenerateTokenAsync(user)).AccessToken;

        // Act
        var isValid = _jwtService.ValidateToken(expiredToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid.token.format")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid")]
    [InlineData(null)]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse(string? invalidToken)
    {
        var isValid = _jwtService.ValidateToken(invalidToken);
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetClaimsFromToken_WithValidToken_ShouldReturnClaims()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .WithRole(UserRole.Doctor)
            .Build();

        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Act
        var claims = _jwtService.GetClaimsFromToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims.Should().NotBeEmpty();

        var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be(user.Id.ToString());

        var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Act
        var userId = _jwtService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetUserRoleFromToken_WithValidToken_ShouldReturnRole()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithRole(UserRole.Doctor)
            .Build();
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        // Act
        var role = _jwtService.GetUserRoleFromToken(token);

        // Assert
        role.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueToken()
    {
        var refreshToken1 = _jwtService.GenerateRefreshToken();
        var refreshToken2 = _jwtService.GenerateRefreshToken();

        refreshToken1.Should().NotBeNullOrEmpty();
        refreshToken2.Should().NotBeNullOrEmpty();
        refreshToken1.Should().NotBe(refreshToken2);
        refreshToken1.Length.Should().BeGreaterOrEqualTo(32);
    }

    [Fact]
    public async Task IsTokenExpired_WithExpiredToken_ShouldReturnTrue()
    {
        var expiredSettings = new JwtSettings
        {
            SecretKey = "TestSecretKeyForPoliCareJwtTokenGenerationInTestEnvironmentOnly123456789",
            Issuer = "PoliCareTest",
            Audience = "PoliCareTestUsers",
            ExpiryInMinutes = -60,
            RefreshTokenExpiryInDays = 7
        };

        var mockExpiredOptions = new Mock<IOptions<JwtSettings>>();
        mockExpiredOptions.Setup(x => x.Value).Returns(expiredSettings);

        var expiredService = new JwtService(mockExpiredOptions.Object);
        var user = _dataBuilder.CreateUser().Build();
        var expiredToken = (await expiredService.GenerateTokenAsync(user)).AccessToken;

        var isExpired = _jwtService.IsTokenExpired(expiredToken);

        isExpired.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenExpired_WithValidToken_ShouldReturnFalse()
    {
        var user = _dataBuilder.CreateUser().Build();
        var validToken = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        var isExpired = _jwtService.IsTokenExpired(validToken);

        isExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GetTokenExpirationDate_ShouldReturnCorrectDate()
    {
        var user = _dataBuilder.CreateUser().Build();
        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;
        var expectedExpiration = DateTime.UtcNow.AddMinutes(60);

        var actualExpiration = _jwtService.GetTokenExpirationDate(token);

        actualExpiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GenerateToken_WithBlockedUser_ShouldIncludeBlockedStatus()
    {
        var user = _dataBuilder.CreateUser()
            .Blocked("Test block reason")
            .Build();

        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var blockedClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_blocked");
        blockedClaim.Should().NotBeNull();
        blockedClaim!.Value.Should().Be("True");
    }

    [Fact]
    public async Task TokenShouldIncludeSecurityClaims()
    {
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .Build();

        var token = (await _jwtService.GenerateTokenAsync(user)).AccessToken;

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var claims = jwtToken.Claims.ToList();

        claims.Should().Contain(c => c.Type == "jti");
        claims.Should().Contain(c => c.Type == "iat");
        claims.Should().Contain(c => c.Type == "nbf");
    }
}

/// <summary>
/// Classe di supporto per impostazioni JWT nei test
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; }
    public int RefreshTokenExpiryInDays { get; set; }
}