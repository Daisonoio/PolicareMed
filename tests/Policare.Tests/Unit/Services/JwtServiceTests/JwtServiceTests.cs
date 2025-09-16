using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using PoliCare.Services.Interfaces; // Per JwtSettings e IJwtService
using PoliCare.Services.Services;
using PoliCare.Tests.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace PoliCare.Tests.Unit.Services.JwtServiceTests;

/// <summary>
/// Test per JwtService - Gestione token JWT per autenticazione
/// Aggiornato per il JwtService reale con UnitOfWork e Logger
/// </summary>
public class JwtServiceTests
{
    private readonly TestDataBuilder _dataBuilder;
    private readonly JwtService _jwtService;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtOptions;
    private readonly Mock<ILogger<JwtService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public JwtServiceTests()
    {
        _dataBuilder = new TestDataBuilder();

        // Setup JWT Settings per test - Usa la classe reale dal progetto Services
        var jwtSettings = new JwtSettings
        {
            SecretKey = "TestSecretKeyForPoliCareJwtTokenGenerationInTestEnvironmentOnly123456789",
            Issuer = "PoliCareTest",
            Audience = "PoliCareTestUsers",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
            EnableRefreshToken = true,
            RememberMeExpiryDays = 30
        };

        _mockJwtOptions = new Mock<IOptions<JwtSettings>>();
        _mockJwtOptions.Setup(x => x.Value).Returns(jwtSettings);

        _mockLogger = new Mock<ILogger<JwtService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Setup mock per UserSession repository
        var mockUserSessionRepo = new Mock<IRepository<UserSession>>();
        _mockUnitOfWork.Setup(x => x.Repository<UserSession>()).Returns(mockUserSessionRepo.Object);
        _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

        _jwtService = new JwtService(_mockUnitOfWork.Object, _mockLogger.Object, _mockJwtOptions.Object);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithValidUser_ShouldReturnValidJWT()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .WithRole(UserRole.Doctor)
            .Build();

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        tokenResult.Should().NotBeNull();
        tokenResult.AccessToken.Should().NotBeNullOrEmpty();
        tokenResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verifica che sia un JWT valido
        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.CanReadToken(tokenResult.AccessToken).Should().BeTrue();

        var jwtToken = tokenHandler.ReadJwtToken(tokenResult.AccessToken);
        jwtToken.Should().NotBeNull();
        jwtToken.Header.Alg.Should().Be("HS256");
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .WithRole(UserRole.Doctor)
            .WithClinic(Guid.NewGuid())
            .Build();

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenResult.AccessToken);

        var claims = jwtToken.Claims.ToList();

        // Verifica claims obbligatori (basati sui claim nel JwtService reale)
        claims.Should().Contain(c => c.Type == "userId" && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == $"{user.FirstName} {user.LastName}");
        claims.Should().Contain(c => c.Type == "clinicId" && c.Value == user.ClinicId.ToString());

        // Verifica issuer e audience
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
    public async Task GenerateTokenAsync_WithDifferentRoles_ShouldIncludeCorrectRole(UserRole role)
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithRole(role)
            .Build();

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenResult.AccessToken);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(role.ToString());
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldSetCorrectExpiration()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        tokenResult.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenResult.AccessToken);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GenerateTokenAsync_WithRememberMe_ShouldExtendExpiration()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var expectedExpiryRememberMe = DateTime.UtcNow.AddDays(30); // RememberMeExpiryDays

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user, rememberMe: true);

        // Assert
        tokenResult.ExpiresAt.Should().BeCloseTo(expectedExpiryRememberMe, TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Act
        var principal = _jwtService.ValidateToken(tokenResult.AccessToken);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid.token.format")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid")]
    [InlineData(null)]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull(string? invalidToken)
    {
        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }





    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var tokenResult = _jwtService.GenerateTokenAsync(user).Result;

        // Act
        var isValid = _jwtService.ValidateToken(tokenResult.AccessToken);

        // Assert
        isValid.Should().Be(true); // <-- ERRORE: BeTrue non esiste su ObjectAssertions

        // FIX: Sostituisci con Should().BeTrue() su BooleanAssertions
        isValid.Should().Be(true);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid.token.format")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid")]
    [InlineData(null)]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse(string? invalidToken)
    {
        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        isValid.Should().Be(false); // <-- ERRORE: BeFalse non esiste su ObjectAssertions

        // FIX: Sostituisci con Should().BeFalse() su BooleanAssertions
        isValid.Should().Be(false);
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

        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Act
        var claims = _jwtService.GetClaimsFromToken(tokenResult.AccessToken);

        // Assert
        claims.Should().NotBeNull();
        claims.Should().NotBeEmpty();

        var userIdClaim = claims.FirstOrDefault(c => c.Type == "userId");
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
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Act
        var userId = _jwtService.GetUserIdFromToken(tokenResult.AccessToken);

        // Assert
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueToken()
    {
        // Act
        var refreshToken1 = _jwtService.GenerateRefreshToken();
        var refreshToken2 = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken1.Should().NotBeNullOrEmpty();
        refreshToken2.Should().NotBeNullOrEmpty();
        refreshToken1.Should().NotBe(refreshToken2);

        // Refresh token dovrebbe essere sufficientemente lungo per sicurezza
        refreshToken1.Length.Should().BeGreaterOrEqualTo(32);
    }

    [Fact]
    public void IsTokenExpired_WithExpiredToken_ShouldReturnTrue()
    {
        // Arrange - Token con scadenza nel passato
        var expiredSettings = new JwtSettings
        {
            SecretKey = "TestSecretKeyForPoliCareJwtTokenGenerationInTestEnvironmentOnly123456789",
            Issuer = "PoliCareTest",
            Audience = "PoliCareTestUsers",
            ExpiryMinutes = -60, // 1 ora fa
            RefreshTokenExpiryDays = 7,
            EnableRefreshToken = true,
            RememberMeExpiryDays = 30
        };

        var mockExpiredOptions = new Mock<IOptions<JwtSettings>>();
        mockExpiredOptions.Setup(x => x.Value).Returns(expiredSettings);

        var expiredService = new JwtService(_mockUnitOfWork.Object, _mockLogger.Object, mockExpiredOptions.Object);
        var user = _dataBuilder.CreateUser().Build();
        var expiredTokenResult = expiredService.GenerateTokenAsync(user).Result;

        // Act
        var isExpired = _jwtService.IsTokenExpired(expiredTokenResult.AccessToken);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenExpired_WithValidToken_ShouldReturnFalse()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var validTokenResult = await _jwtService.GenerateTokenAsync(user);

        // Act
        var isExpired = _jwtService.IsTokenExpired(validTokenResult.AccessToken);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateTokenAsync_WithBlockedUser_ShouldIncludeBlockedStatus()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .Blocked("Test block reason")
            .Build();

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenResult.AccessToken);

        var blockedClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "isBlocked");
        blockedClaim.Should().NotBeNull();
        blockedClaim!.Value.Should().Be("True");
    }

    [Fact]
    public async Task TokenShouldIncludeSecurityClaims()
    {
        // Arrange
        var user = _dataBuilder.CreateUser()
            .WithName("Mario", "Rossi")
            .WithEmail("mario.rossi@test.com")
            .Build();

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenResult.AccessToken);
        var claims = jwtToken.Claims.ToList();

        // Verifica claims di sicurezza
        claims.Should().Contain(c => c.Type == "jti"); // JWT ID per revoca
        claims.Should().Contain(c => c.Type == "iat"); // Issued At
        claims.Should().Contain(c => c.Type == "nbf"); // Not Before
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldCreateUserSession()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var mockUserSessionRepo = new Mock<IRepository<UserSession>>();
        _mockUnitOfWork.Setup(x => x.Repository<UserSession>()).Returns(mockUserSessionRepo.Object);

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        mockUserSessionRepo.Verify(x => x.AddAsync(It.IsAny<UserSession>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithRefreshTokenEnabled_ShouldReturnRefreshToken()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();

        // Act
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        // Assert
        tokenResult.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeSuccessfully()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var tokenResult = await _jwtService.GenerateTokenAsync(user);

        var mockUserSessionRepo = new Mock<IRepository<UserSession>>();
        var existingSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "test_hash",
            IsActive = true,
            IsRevoked = false
        };

        mockUserSessionRepo.Setup(x => x.GetWhereAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSession, bool>>>()))
                          .ReturnsAsync(new[] { existingSession });

        _mockUnitOfWork.Setup(x => x.Repository<UserSession>()).Returns(mockUserSessionRepo.Object);

        // Act
        await _jwtService.RevokeTokenAsync(tokenResult.AccessToken);

        // Assert
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidRefreshToken_ShouldReturnNewToken()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();
        var originalTokenResult = await _jwtService.GenerateTokenAsync(user);

        var mockUserSessionRepo = new Mock<IRepository<UserSession>>();
        var existingSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = "refresh_hash",
            IsActive = true,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        mockUserSessionRepo.Setup(x => x.GetWhereAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSession, bool>>>()))
                          .ReturnsAsync(new[] { existingSession });

        var mockUserRepo = new Mock<IRepository<User>>();
        mockUserRepo.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.Repository<UserSession>()).Returns(mockUserSessionRepo.Object);
        _mockUnitOfWork.Setup(x => x.Repository<User>()).Returns(mockUserRepo.Object);

        // Act
        var newTokenResult = await _jwtService.RefreshTokenAsync(originalTokenResult.RefreshToken!);

        // Assert
        newTokenResult.Should().NotBeNull();
        newTokenResult.AccessToken.Should().NotBeNullOrEmpty();
        newTokenResult.AccessToken.Should().NotBe(originalTokenResult.AccessToken);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ForUser_ShouldReturnActiveSessions()
    {
        // Arrange
        var user = _dataBuilder.CreateUser().Build();

        var activeSessions = new List<UserSession>
        {
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IsActive = true,
                IsRevoked = false,
                StartedAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IsActive = true,
                IsRevoked = false,
                StartedAt = DateTime.UtcNow.AddMinutes(-15)
            }
        };

        var mockUserSessionRepo = new Mock<IRepository<UserSession>>();
        mockUserSessionRepo.Setup(x => x.GetWhereAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSession, bool>>>()))
                          .ReturnsAsync(activeSessions);

        _mockUnitOfWork.Setup(x => x.Repository<UserSession>()).Returns(mockUserSessionRepo.Object);

        // Act
        var sessions = await _jwtService.GetActiveSessionsAsync(user.Id);

        // Assert
        sessions.Should().HaveCount(2);
        sessions.Should().AllSatisfy(s =>
        {
            s.UserId.Should().Be(user.Id);
            s.IsActive.Should().BeTrue();
            s.IsRevoked.Should().BeFalse();
        });
    }



    [Fact]
    public async Task CleanupExpiredSessionsAsync_ShouldRemoveExpiredSessions()
    {
        // Arrange
        var expiredSessions = new List<UserSession>
        {
            new UserSession
            {
                Id = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // Scaduta ieri
                IsActive = true
            },
            new UserSession
            {
                Id = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(-7), // Scaduta 7 giorni fa
                IsActive = false
            }
        };

        var mockUserSessionRepo = new Mock<IRepository<UserSession>>();
        mockUserSessionRepo.Setup(x => x.GetWhereAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSession, bool>>>()))
                          .ReturnsAsync(expiredSessions);

        _mockUnitOfWork.Setup(x => x.Repository<UserSession>()).Returns(mockUserSessionRepo.Object);

        // Act
        await _jwtService.CleanupExpiredSessionsAsync();

        // Assert
        mockUserSessionRepo.Verify(x => x.DeleteRangeAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserSession, bool>>>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
    }
}
