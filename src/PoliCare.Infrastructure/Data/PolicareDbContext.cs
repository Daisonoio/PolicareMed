using Microsoft.EntityFrameworkCore;
using PoliCare.Core.Entities;

namespace PoliCare.Infrastructure.Data;

public class PoliCareDbContext : DbContext
{
    public PoliCareDbContext(DbContextOptions<PoliCareDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<ClinicSecurity> ClinicSecurities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CONFIGURAZIONI ESISTENTI - Mantenute inalterate

        // Clinic configuration
        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasIndex(e => e.VatNumber).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Settings).HasDefaultValue("{}");
        });

        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => new { e.ClinicId, e.FiscalCode }).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => new { e.ClinicId, e.LastName, e.FirstName });

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Patients)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Doctor configuration
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasIndex(e => e.LicenseNumber).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Doctors)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
            entity.Property(e => e.CommissionPercentage).HasPrecision(5, 4);
            entity.Property(e => e.WorkingHours).HasDefaultValue("{}");
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Users)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => new { e.ClinicId, e.Code }).IsUnique();

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Rooms)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Appointment configuration
        modelBuilder.Entity<Appointment>(entity =>
        {
            // Indici per performance
            entity.HasIndex(e => new { e.DoctorId, e.StartTime });
            entity.HasIndex(e => new { e.RoomId, e.StartTime });
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Status);

            // Relationship con Patient
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship con Doctor
            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship con Room
            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing relationship per follow-up
            entity.HasOne(e => e.ParentAppointment)
                .WithMany(p => p.FollowUpAppointments)
                .HasForeignKey(e => e.ParentAppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precision per Price
            entity.Property(e => e.Price).HasPrecision(10, 2);
        });

        // TimeSlot configuration
        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasIndex(e => new { e.DoctorId, e.DayOfWeek, e.StartTime });

            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.TimeSlots)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MedicalRecord configuration
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Attachments).HasDefaultValue("[]");
        });

        // NUOVE CONFIGURAZIONI - Aggiunte per Security Enhancement

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurazioni precisione decimali per coordinate
            entity.Property(e => e.Latitude).HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasPrecision(10, 7);

            // Valori di default
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.IsSuspicious).HasDefaultValue(false);
            entity.Property(e => e.RequestCount).HasDefaultValue(0);
        });

        // ClinicSecurity configuration
        modelBuilder.Entity<ClinicSecurity>(entity =>
        {
            entity.HasIndex(e => e.ClinicId).IsUnique(); // 1:1 relationship

            entity.HasOne(e => e.Clinic)
                .WithOne()
                .HasForeignKey<ClinicSecurity>(cs => cs.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurazioni precisione decimali
            entity.Property(e => e.LastPaymentAmount).HasPrecision(10, 2);

            // Valori di default per nuove colonne
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSuspended).HasDefaultValue(false);
            entity.Property(e => e.SubscriptionPlan).HasDefaultValue("Basic");
            entity.Property(e => e.SubscriptionStatus).HasDefaultValue(SubscriptionStatus.Trial);
            entity.Property(e => e.GracePeriodDays).HasDefaultValue(7);
            entity.Property(e => e.PaymentBlocked).HasDefaultValue(false);
            entity.Property(e => e.MaxUsers).HasDefaultValue(5);
            entity.Property(e => e.MaxPatients).HasDefaultValue(1000);
            entity.Property(e => e.MaxAppointmentsPerMonth).HasDefaultValue(500);
            entity.Property(e => e.MaxStorageMB).HasDefaultValue(1000);
            entity.Property(e => e.CurrentStorageUsageMB).HasDefaultValue(0);
            entity.Property(e => e.IPRestrictionsEnabled).HasDefaultValue(false);
            entity.Property(e => e.GeoRestrictionsEnabled).HasDefaultValue(false);
            entity.Property(e => e.AccessHoursRestricted).HasDefaultValue(false);
            entity.Property(e => e.PasswordMinLength).HasDefaultValue(8);
            entity.Property(e => e.PasswordRequireUppercase).HasDefaultValue(true);
            entity.Property(e => e.PasswordRequireLowercase).HasDefaultValue(true);
            entity.Property(e => e.PasswordRequireDigits).HasDefaultValue(true);
            entity.Property(e => e.PasswordRequireSpecialChars).HasDefaultValue(true);
            entity.Property(e => e.PasswordExpiryDays).HasDefaultValue(90);
            entity.Property(e => e.MaxLoginAttempts).HasDefaultValue(5);
            entity.Property(e => e.LockoutDurationMinutes).HasDefaultValue(30);
            entity.Property(e => e.SessionDurationHours).HasDefaultValue(8);
            entity.Property(e => e.MaxInactivityMinutes).HasDefaultValue(60);
            entity.Property(e => e.AuditLoggingEnabled).HasDefaultValue(true);
            entity.Property(e => e.LogRetentionDays).HasDefaultValue(365);
            entity.Property(e => e.SecurityNotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.AutoBackupEnabled).HasDefaultValue(true);
            entity.Property(e => e.BackupFrequencyDays).HasDefaultValue(1);
            entity.Property(e => e.BackupRetentionDays).HasDefaultValue(30);
        });
    }
}