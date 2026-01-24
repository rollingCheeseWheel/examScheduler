using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace examScheduler.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<UserProfile, IdentityRole<Guid>, Guid>(options)
{
	public DbSet<AuditLog> AuditLogs { get; set; }
	public DbSet<Calendar> Calendars { get; set; }
	public DbSet<Classroom> Classrooms { get; set; }
	public DbSet<Lesson> Lessons { get; set; }
	public DbSet<RefreshTokenSession> RefreshSessions { get; set; }
	public DbSet<Schedule> Schedules { get; set; }
	public DbSet<School> Schools { get; set; }
	public DbSet<StudentProfile> StudentProfiles { get; set; }
	public DbSet<Subject> Subjects { get; set; }
	public DbSet<SwapRequest> SwapRequests { get; set; }
	public DbSet<Teacher> Teachers { get; set; }
	public DbSet<TeacherProfile> TeacherProfiles { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		#region School
		modelBuilder.Entity<School>()
			.HasIndex(s => s.RegisterUri)
			.IsUnique();
		#endregion

		#region Classroom
		modelBuilder.Entity<Classroom>()
			.HasIndex(c => new { c.SchoolId, c.Name })
			.IsUnique();

		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.School)
			.WithMany()
			.HasForeignKey(c => c.SchoolId);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Students)
			.WithOne(s => s.Classroom)
			.HasForeignKey(s => s.ClassroomId);

		modelBuilder.Entity<Classroom>()
			.Navigation(c => c.Calendar)
			.AutoInclude();
		#endregion

		#region Calendar
		modelBuilder.Entity<Calendar>()
			.HasMany(c => c.Lessons)
			.WithOne();

		modelBuilder.Entity<Calendar>()
			.Navigation(t => t.Lessons)
			.AutoInclude();
		#endregion

		#region Lesson
		modelBuilder.Entity<Lesson>()
			.HasMany(l => l.Teachers)
			.WithMany();

		modelBuilder.Entity<Lesson>()
			.HasOne(l => l.Subject)
			.WithMany();
		#endregion

		#region Teacher
		modelBuilder.Entity<Teacher>()
			.HasMany(t => t.Subjects)
			.WithMany();

		//modelBuilder.Entity<Teacher>()
		//	.HasMany(t => t.Classrooms)
		//	.WithMany(c => c.Teachers);

		modelBuilder.Entity<Teacher>()
			.Navigation(t => t.Subjects)
			.AutoInclude();

		modelBuilder.Entity<Teacher>()
			.HasOne(t => t.School)
			.WithMany()
			.HasForeignKey(t => t.SchoolId);
		#endregion

		#region Subject

		#endregion

		#region Schedule
		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.ExamSlots)
			.WithOne(e => e.Schedule)
			.HasForeignKey(e => e.ScheduleId);

		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.AuditLogs)
			.WithOne();

		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.ExamSlots)
			.AutoInclude();
		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.AuditLogs)
			.AutoInclude();
		#endregion

		#region ExamSlot
		modelBuilder.Entity<ExamSlot>()
			.HasMany(e => e.Participants)
			.WithMany();

		modelBuilder.Entity<ExamSlot>()
			.HasMany(e => e.ActuallyParticipated)
			.WithMany();

		modelBuilder.Entity<ExamSlot>()
			.Navigation(s => s.Participants)
			.AutoInclude();
		modelBuilder.Entity<ExamSlot>()
			.Navigation(s => s.ActuallyParticipated)
			.AutoInclude();
		#endregion

		#region AuditLog

		#endregion

		#region SwapRequest
		modelBuilder.Entity<SwapRequest>()
			.HasIndex(sr => new { sr.ScheduleId, sr.RequestingStudentId })
			.IsUnique();

		modelBuilder.Entity<SwapRequest>()
			.HasIndex(sr => new { sr.ScheduleId, sr.RequestedStudentId })
			.IsUnique();
		#endregion

		#region UserProfile
		modelBuilder.Entity<UserProfile>()
			.HasIndex(u => new
			{
				u.UserName,
				u.SchoolId,
			})
			.IsUnique();

		modelBuilder.Entity<UserProfile>()
			.HasOne(up => up.StudentProfile)
			.WithOne(sp => sp.UserProfile)
			.HasForeignKey<StudentProfile>(sp => sp.Id);

		modelBuilder.Entity<UserProfile>()
			.HasOne(up => up.TeacherProfile)
			.WithOne(tp => tp.UserProfile)
			.HasForeignKey<TeacherProfile>(tp => tp.Id);

		modelBuilder.Entity<UserProfile>()
			.HasOne(u => u.School)
			.WithMany()
			.HasForeignKey(u => u.SchoolId);
		#endregion

		#region StudentProfile
		modelBuilder.Entity<StudentProfile>()
			.Navigation(sp => sp.UserProfile)
			.AutoInclude();
		#endregion

		#region TeacherProfile
		modelBuilder.Entity<TeacherProfile>()
			.HasOne(tp => tp.Teacher)
			.WithOne(t => t.TeacherProfile)
			.HasForeignKey<TeacherProfile>(tp => tp.TeacherId);

		modelBuilder.Entity<TeacherProfile>()
			.HasMany(tp => tp.Classrooms)
			.WithMany();

		modelBuilder.Entity<TeacherProfile>()
			.Navigation(tp => tp.Teacher)
			.AutoInclude();
		#endregion

		#region RefreshSession
		modelBuilder.Entity<RefreshTokenSession>()
			.HasIndex(s => s.TokenValue)
			.IsUnique();
		#endregion
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
		IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entires = ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified);

		foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entry in entires)
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
