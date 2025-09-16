using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoliCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VatNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Settings = table.Column<string>(type: "longtext", nullable: false, defaultValue: "{}")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClinicSecurities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClinicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsSuspended = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SuspensionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SuspendedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SuspendedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SuspensionExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SubscriptionPlan = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Basic")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubscriptionStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SubscriptionStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SubscriptionEndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    PaymentBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    PaymentBlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastPaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastPaymentAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    MaxUsers = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    MaxPatients = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    MaxAppointmentsPerMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 500),
                    MaxStorageMB = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    CurrentStorageUsageMB = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IPRestrictionsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AllowedIPAddresses = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockedIPAddresses = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoRestrictionsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AllowedCountries = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessHoursRestricted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AccessStartTime = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessEndTime = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllowedDaysOfWeek = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordMinLength = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    PasswordRequireUppercase = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PasswordRequireLowercase = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PasswordRequireDigits = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PasswordRequireSpecialChars = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PasswordExpiryDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    MaxLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    LockoutDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    SessionDurationHours = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    MaxInactivityMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    AuditLoggingEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    LogRetentionDays = table.Column<int>(type: "int", nullable: false, defaultValue: 365),
                    SecurityNotificationsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SecurityNotificationEmail = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutoBackupEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    BackupFrequencyDays = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BackupRetentionDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    LastBackupDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSecurities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicSecurities_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClinicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FiscalCode = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MedicalHistory = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Preferences = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ClinicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<int>(type: "int", nullable: false),
                    PasswordSalt = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ClinicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PrimaryClinicId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastLoginIP = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LastFailedLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    BlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BlockExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordResetToken = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordResetTokenExpires = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TimeZone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredLanguage = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserSettings = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BackupCodes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentSuspended = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PaymentSuspendedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Clinics_PrimaryClinicId",
                        column: x => x.PrimaryClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClinicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Specialization = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LicenseNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CommissionPercentage = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    WorkingHours = table.Column<string>(type: "longtext", nullable: false, defaultValue: "{}")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doctors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TokenHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsRevoked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RevokeReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperatingSystem = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Browser = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Region = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    IsSuspicious = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SuspiciousReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastRequestAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastEndpoint = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PatientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DoctorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServiceType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    IsPaid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RecurringGroupId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ParentAppointmentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Appointments_ParentAppointmentId",
                        column: x => x.ParentAppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DoctorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RecurrenceWeeks = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeSlots_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeSlots_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PatientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DoctorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppointmentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Attachments = table.Column<string>(type: "longtext", nullable: false, defaultValue: "[]")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSharedWithPatient = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ParentAppointmentId",
                table: "Appointments",
                column: "ParentAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId_StartTime",
                table: "Appointments",
                columns: new[] { "RoomId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Email",
                table: "Clinics",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_VatNumber",
                table: "Clinics",
                column: "VatNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSecurities_ClinicId",
                table: "ClinicSecurities",
                column: "ClinicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_ClinicId",
                table: "Doctors",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_LicenseNumber",
                table: "Doctors",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_AppointmentId",
                table: "MedicalRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_DoctorId",
                table: "MedicalRecords",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId_FiscalCode",
                table: "Patients",
                columns: new[] { "ClinicId", "FiscalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId_LastName_FirstName",
                table: "Patients",
                columns: new[] { "ClinicId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Email",
                table: "Patients",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_ClinicId_Code",
                table: "Rooms",
                columns: new[] { "ClinicId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_DoctorId_DayOfWeek_StartTime",
                table: "TimeSlots",
                columns: new[] { "DoctorId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_RoomId",
                table: "TimeSlots",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClinicId",
                table: "Users",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PrimaryClinicId",
                table: "Users",
                column: "PrimaryClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_TokenHash",
                table: "UserSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsActive",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicSecurities");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Clinics");
        }
    }
}
