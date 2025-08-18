using Microsoft.EntityFrameworkCore;
using PoliCare.Core.Entities;

namespace PoliCare.Infrastructure.Data;

public class PoliCareDbContext : DbContext
{
    public PoliCareDbContext(DbContextOptions<PoliCareDbContext> options)
        : base(options)
    {
    }

    // DbSets - SOLO entità che manteniamo
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }

    // ✅ FIX PERMANENTE UTC: Override SaveChanges per garantire UTC
    public override int SaveChanges()
    {
        EnsureUtcTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureUtcTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    // ✅ FIX PERMANENTE UTC: Metodo per garantire che tutti i DateTime siano UTC
    private void EnsureUtcTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;

                case EntityState.Deleted:
                    // Soft delete implementation
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        // ✅ FIX PERMANENTE UTC: Converti tutti i DateTime Unspecified in UTC
        foreach (var entry in ChangeTracker.Entries())
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime && dateTime.Kind == DateTimeKind.Unspecified)
                {
                    property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // Room configuration - SENZA navigation verso RoomAvailability
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => new { e.ClinicId, e.Code }).IsUnique();

            entity.HasOne(e => e.Clinic)
                .WithMany(c => c.Rooms)
                .HasForeignKey(e => e.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Appointment configuration - CORRETTA per evitare shadow properties
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

            // Relationship con Doctor - ESPLICITA
            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship con Room - ESPLICITA e senza collection navigation
            entity.HasOne(e => e.Room)
                .WithMany() // Nessuna collection navigation in Room
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

            // TimeSlot relationship con Room - SEMPLIFICATA
            entity.HasOne(e => e.Room)
                .WithMany() // Nessuna collection navigation in Room
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MedicalRecord configuration
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.AppointmentId);

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany() // Nessuna collection navigation in Doctor per MedicalRecords
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Appointment)
                .WithMany() // Nessuna collection navigation in Appointment
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Attachments).HasDefaultValue("[]");
        });

        // Soft delete global query filters
        modelBuilder.Entity<Clinic>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Doctor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Room>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TimeSlot>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(e => !e.IsDeleted);
    }
}