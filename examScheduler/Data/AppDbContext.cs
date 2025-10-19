using Entities;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.Calendar)
			.WithOne(t => t.Classroom)
			.HasForeignKey<Calendar>(t => t.ClassroomId);

		modelBuilder.Entity<Student>()
			.HasIndex(s => s.RegisterUsername)
			.IsUnique();

		modelBuilder.Entity<Student>()
			.HasMany(s => s.ExamSlots)
			.WithMany(e => e.Participants);
	}

	public DbSet<AuditLog> AuditLogs { get; set; }
	public DbSet<Classroom> Classrooms { get; set; }
	public DbSet<Schedule> Schedules { get; set; }
	public DbSet<Student> Students { get; set; }
	public DbSet<Teacher> Teachers { get; set; }
	public DbSet<Calendar> Timetables { get; set; }
	public DbSet<Subject> Subjects { get; set; }
}
