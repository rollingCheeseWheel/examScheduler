using examScheduler.Entities;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.Timetable)
			.WithOne(t => t.Classroom)
			.HasForeignKey<Timetable>(t => t.ClassroomId);
	}

	public DbSet<AuditLog> AuditLogs { get; set; }
	public DbSet<Classroom> Classrooms { get; set; }
	public DbSet<Schedule> Schedules { get; set; }
	public DbSet<Student> Students { get; set; }
	public DbSet<Teacher> Teachers { get; set; }
	public DbSet<Timetable> Timetables { get; set; }
	public DbSet<Subject> Subjects { get; set; }

}
