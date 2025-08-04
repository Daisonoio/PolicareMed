// PoliCare.Infrastructure/Data/PoliCareDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PoliCare.Core.Entities;

namespace PoliCare.Infrastructure.Data;

public class PoliCareDbContext : DbContext
{
    public PoliCareDbContext(DbContextOptions<PoliCareDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Professional> Professionals { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<RoomAvailability> RoomAvailabilities { get; set; }
    public DbSet<ProfessionalAvailability> ProfessionalAvailabilities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clinic configuration
        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasIndex(e => e.VatNumber).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => new { e.ClinicId, e.FiscalCode }).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => new { e.ClinicId, e.LastName, e.FirstName });
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

        // Professional configuration
        modelBuilder.Entity<Professional>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.LicenseNumber);
            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Professionals)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Appointment configuration
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(e => new { e.ProfessionalId, e.StartTime });
            entity.HasIndex(e => new { e.RoomId, e.StartTime });
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Professional)
                .WithMany(p => p.Appointments)
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Appointments)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParentAppointment)
                .WithMany(p => p.FollowUpAppointments)
                .HasForeignKey(e => e.ParentAppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Price).HasPrecision(10, 2);
        });

        // RoomAvailability configuration
        modelBuilder.Entity<RoomAvailability>(entity =>
        {
            entity.HasIndex(e => new { e.RoomId, e.DayOfWeek });
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Availabilities)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProfessionalAvailability configuration
        modelBuilder.Entity<ProfessionalAvailability>(entity =>
        {
            entity.HasIndex(e => new { e.ProfessionalId, e.DayOfWeek });
            entity.HasOne(e => e.Professional)
                .WithMany(p => p.Availabilities)
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Soft delete global query filter
        modelBuilder.Entity<Clinic>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Room>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Professional>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RoomAvailability>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProfessionalAvailability>().HasQueryFilter(e => !e.IsDeleted);
    }
}