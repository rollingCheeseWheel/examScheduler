using Entities;
using Models.DigitalesRegister;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.Timetable)
			.WithOne(t => t.Classroom)
			.HasForeignKey<Timetable>(t => t.ClassroomId);

		modelBuilder.Entity<Student>()
			.HasIndex(s => s.RegisterUsername)
			.IsUnique();
		modelBuilder.Entity<Student>()
			.HasIndex(s => s.Salt)
			.IsUnique();
	}

	public DbSet<AuditLog> AuditLogs { get; set; }
	public DbSet<Classroom> Classrooms { get; set; }
	public DbSet<Schedule> Schedules { get; set; }
	public DbSet<Student> Students { get; set; }
	public DbSet<Entities.Teacher> Teachers { get; set; }
	public DbSet<Timetable> Timetables { get; set; }
	public DbSet<Subject> Subjects { get; set; }
}
