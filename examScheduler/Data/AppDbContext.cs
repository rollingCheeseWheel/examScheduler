using Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

		modelBuilder.Entity<Teacher>()
			.HasOne(t => t.TeacherProfile)
			.WithOne(t => t.Teacher)
			.HasForeignKey<TeacherProfile>(t => t.TeacherId);

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
			.HasIndex(s => new
			{
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
			.HasIndex(c => new
			{
				c.RegisterId,
				c.RegisterUri,
			})
			.IsUnique();
	}

	public override int SaveChanges()
	{
		ValidateEntities();
		return base.SaveChanges();
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		ValidateEntities();
		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		ValidateEntities();
		return base.SaveChangesAsync(cancellationToken);
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		ValidateEntities();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	private void ValidateEntities()
	{
		var entires = ChangeTracker.Entries()
			.Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

		foreach (var entry in entires)
		{
			var entity = entry.Entity;
			var validationContext = new ValidationContext(entity);
			var validationResults = new List<ValidationResult>();

			if (!Validator.TryValidateObject(entity, validationContext, validationResults, true))
			{
				var errorMessages = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
				throw new ValidationException($"Validation failed for {entity.GetType().Name}: {errorMessages}");
			}
		}
	}
}
