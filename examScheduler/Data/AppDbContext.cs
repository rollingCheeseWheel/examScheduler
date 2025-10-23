using Entities;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<AuditLog> AuditLogs { get; set; }
	public DbSet<Classroom> Classrooms { get; set; }
	public DbSet<Schedule> Schedules { get; set; }
	public DbSet<Student> Students { get; set; }
	public DbSet<Teacher> Teachers { get; set; }
	public DbSet<Calendar> Timetables { get; set; }
	public DbSet<Subject> Subjects { get; set; }
	public DbSet<UserProfile> UserProfiles { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.Calendar)
			.WithOne(t => t.Classroom)
			.HasForeignKey<Calendar>(t => t.ClassroomId);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Teachers)
			.WithMany(t => t.Classrooms);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Schedules)
			.WithOne(s => s.Classroom);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Students)
			.WithOne(s => s.Classroom);

		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.ExamSlots)
			.WithOne(e => e.Schedule);

		modelBuilder.Entity<ExamSlot>()
			.HasMany(e => e.Participants)
			.WithMany(s => s.ExamSlots);

		modelBuilder.Entity<UserProfile>()
			.HasIndex(s => new {
				s.RegisterUsername,
				s.RegisterUri,
				s.RegisterId
			})
			.IsUnique();

		modelBuilder.Entity<Teacher>()
			.HasMany(t => t.Lessons)
			.WithMany(l => l.Teachers);

		modelBuilder.Entity<Subject>()
			.HasIndex(s => new
			{
				s.RegisterId,
				s.Name,
			})
			.IsUnique();

		modelBuilder.Entity<Classroom>()
			.HasIndex(c => c.RegisterId)
			.IsUnique();
	}
}
