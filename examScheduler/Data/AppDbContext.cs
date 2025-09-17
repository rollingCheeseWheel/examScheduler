using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<Entities.Classroom> Classrooms { get; set; }
	public DbSet<Entities.Student> Students { get; set; }
	public DbSet<Entities.Schedule> Schedules { get; set; }
}
