using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace examScheduler.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<UserProfile, IdentityRole<Guid>, Guid>(options)
{
	public DbSet<Classroom> Classrooms { get; set; }
	public DbSet<School> Schools { get; set; }
	public DbSet<Subject> Subjects { get; set; }
	public DbSet<Teacher> Teachers { get; set; }

	#region backing DbSets
	public DbSet<AuditLog> _AuditLogs { get; set; }
	public DbSet<Calendar> _Calendars { get; set; }
	public DbSet<ExamSlot> _ExamSlots { get; set; }
	public DbSet<Lesson> _Lessons { get; set; }
	public DbSet<Schedule> _Schedules { get; set; }
	public DbSet<StudentProfile> _StudentProfiles { get; set; }
	public DbSet<TeacherProfile> _TeacherProfiles { get; set; }
	#endregion

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
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<Classroom>()
			.HasIndex(c => new { c.SchoolId, c.Name })
			.IsUnique();

		modelBuilder.Entity<Classroom>()
			.HasOne<School>()
			.WithMany()
			.HasForeignKey(c => c.SchoolId);

		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.Calendar)
			.WithOne(c => c.Classroom)
			.HasForeignKey<Classroom>(c => c.CalendarId);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Students)
			.WithOne(s => s.Classroom)
			.HasForeignKey(s => s.ClassroomId);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Schedules)
			.WithOne(s => s.Classroom)
			.HasForeignKey(s => s.ClassroomId);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Teachers)
			.WithMany(t => t.Classrooms);

		modelBuilder.Entity<Classroom>()
			.Navigation(c => c.Teachers)
			.AutoInclude();

		modelBuilder.Entity<Classroom>()
			.Navigation(c => c.Students)
			.AutoInclude();
		#endregion

		#region Calendar
		modelBuilder.Entity<Calendar>()
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<Calendar>()
			.HasMany(c => c.Lessons)
			.WithOne();

		modelBuilder.Entity<Calendar>()
			.Navigation(t => t.Lessons)
			.AutoInclude();
		#endregion

		#region Lesson
		modelBuilder.Entity<Lesson>()
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<Lesson>()
			.HasMany(l => l.Teachers)
			.WithMany();

		modelBuilder.Entity<Lesson>()
			.HasOne(l => l.Subject)
			.WithMany();

		modelBuilder.Entity<Lesson>()
			.Navigation(l => l.Teachers)
			.AutoInclude();
		modelBuilder.Entity<Lesson>()
			.Navigation(l => l.Subject)
			.AutoInclude();
		#endregion

		#region Teacher
		modelBuilder.Entity<Teacher>()
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<Teacher>()
			.HasMany(t => t.Subjects)
			.WithMany();

		modelBuilder.Entity<Teacher>()
			.HasOne<School>()
			.WithMany()
			.HasForeignKey(t => t.SchoolId);

		modelBuilder.Entity<Teacher>()
			.Navigation(t => t.Subjects)
			.AutoInclude();
		#endregion

		#region Subject

		#endregion

		#region Schedule
		modelBuilder.Entity<Schedule>()
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.ExamSlots)
			.WithOne()
			.HasForeignKey(e => e.ScheduleId);

		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.AuditLogs)
			.WithOne();

		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.Teachers)
			.WithMany();

		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.SwapRequests)
			.WithOne();

		modelBuilder.Entity<Schedule>()
			.HasOne(s => s.ScheduleGenerator)
			.WithOne()
			.HasForeignKey<ScheduleGenerator>(s => s.ScheduleId);

		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.ExamSlots)
			.AutoInclude();
		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.AuditLogs)
			.AutoInclude();
		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.Teachers)
			.AutoInclude();
		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.Subject)
			.AutoInclude();
		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.SwapRequests)
			.AutoInclude();
		modelBuilder.Entity<Schedule>()
			.Navigation(s => s.ScheduleGenerator)
			.AutoInclude();
		#endregion

		#region ExamSlot
		modelBuilder.Entity<ExamSlot>()
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<ExamSlot>()
			.HasMany(e => e.Participants)
			.WithMany();

		modelBuilder.Entity<ExamSlot>()
			.Navigation(s => s.Participants)
			.AutoInclude();
		#endregion

		#region AuditLog
		modelBuilder.Entity<AuditLog>()
			.ConfigureIDGeneratedClientside();
		#endregion

		#region SwapRequest
		modelBuilder.Entity<SwapRequest>()
			.ConfigureIDGeneratedClientside();

		modelBuilder.Entity<SwapRequest>()
			.HasIndex(sr => new { sr.ScheduleId, sr.RequestedSlotId })
			.IsUnique();
		#endregion

		#region ScheduleGenerator
		modelBuilder.Entity<ScheduleGenerator>()
			.HasMany(s => s.GeneratorSlots)
			.WithOne();

		modelBuilder.Entity<ScheduleGenerator>()
			.Navigation(s => s.GeneratorSlots)
			.AutoInclude();
		#endregion

		#region ScheduleGeneratorSlot

		#endregion

		#region UserProfile
		modelBuilder.Entity<UserProfile>()
			.HasIndex(u => new
			{
				u.RegiserId,
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
			.HasOne<School>()
			.WithMany()
			.HasForeignKey(u => u.SchoolId);

		modelBuilder.Entity<UserProfile>()
			.Navigation(u => u.TeacherProfile)
			.AutoInclude();
		modelBuilder.Entity<UserProfile>()
			.Navigation(u => u.StudentProfile)
			.AutoInclude();
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
			.HasForeignKey<Teacher>(t => t.TeacherProfileId);

		modelBuilder.Entity<TeacherProfile>()
			.Navigation(tp => tp.Teacher)
			.AutoInclude();
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
		var entires = ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified);

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
