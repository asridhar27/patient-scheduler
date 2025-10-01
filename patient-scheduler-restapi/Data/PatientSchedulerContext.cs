namespace PatientScheduler.Data
{
    public class PatientSchedulerContext : DbContext
    {
        public PatientSchedulerContext(DbContextOptions<PatientSchedulerContext> options) : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Job> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Patient)
                .WithMany(p => p.Invoices)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Appointment)
                .WithMany(a => a.Invoices)
                .HasForeignKey(i => i.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TimeSlot relationships (kept for future extensibility)
            modelBuilder.Entity<TimeSlot>()
                .HasOne(t => t.Doctor)
                .WithMany()
                .HasForeignKey(t => t.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TimeSlot>()
                .HasOne(t => t.Appointment)
                .WithOne()
                .HasForeignKey<TimeSlot>(t => t.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Doctors
            modelBuilder.Entity<Doctor>().HasData(
                new Doctor
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Smith",
                    Specialization = "Cardiology",
                    Email = "john.smith@hospital.com",
                    PhoneNumber = "555-0101",
                    OfficeAddress = "123 Medical Center Dr",
                    HourlyRate = 200.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Doctor
                {
                    Id = 2,
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Specialization = "Pediatrics",
                    Email = "sarah.johnson@hospital.com",
                    PhoneNumber = "555-0102",
                    OfficeAddress = "456 Children's Hospital Ave",
                    HourlyRate = 180.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Doctor
                {
                    Id = 3,
                    FirstName = "Michael",
                    LastName = "Brown",
                    Specialization = "Orthopedics",
                    Email = "michael.brown@hospital.com",
                    PhoneNumber = "555-0103",
                    OfficeAddress = "789 Sports Medicine Blvd",
                    HourlyRate = 220.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // No time slot seeding — availability is computed dynamically based on office hours and existing appointments
        }
    }
}
