using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Util;

namespace examScheduler.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<UserProfile, IdentityRole<int>, int>(options)
{
	//public DbSet<UserProfile> UserProfiles { get; set; } /* UserManger offers this collection */
	public DbSet<StudentProfile> StudentProfiles { get; set; }
	public DbSet<TeacherProfile> TeacherProfiles { get; set; }
	public DbSet<School> Schools { get; set; }
	public DbSet<Classroom> Classrooms { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<School>(s =>
		{
			s.Property(s => s.RegisterUri)
			  .HasConversion(
				v => v.ToString(),
				v => new Uri(v))
			  .HasMaxLength(2048)
			  .IsRequired();

			s.HasIndex(s => s.RegisterUri).IsUnique();
		});

		// UserProfile
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
			.HasMany(up => up.RefreshTokens)
			.WithOne(rts => rts.UserProfile);

		modelBuilder.Entity<UserProfile>()
			.HasOne(u => u.School)
			.WithMany()
			.HasForeignKey(u => u.SchoolId)
			.OnDelete(DeleteBehavior.Cascade);

		// TeacherProfile
		modelBuilder.Entity<TeacherProfile>()
			.HasOne(tp => tp.Teacher)
			.WithOne(t => t.TeacherProfile)
			.HasForeignKey<TeacherProfile>(tp => tp.TeacherId);

		// Teacher-Subject many-to-many
		modelBuilder.Entity<Teacher>()
			.HasMany(t => t.Subjects)
			.WithMany();

		// Lesson-Teacher many-to-many
		modelBuilder.Entity<Lesson>()
			.HasMany(l => l.Teachers)
			.WithMany();

		// Lesson-Subject: many-to-one
		modelBuilder.Entity<Lesson>()
			.HasOne(l => l.Subject)
			.WithMany();

		// classroom
		modelBuilder.Entity<Classroom>()
			.HasIndex(c => new
			{
				c.RegisterId,
				c.SchoolId,
			})
			.IsUnique();

		modelBuilder.Entity<Classroom>()
			.HasOne(c => c.School)
			.WithMany()
			.HasForeignKey(c => c.SchoolId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Calendars)
			.WithOne(ca => ca.Classroom)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Teachers)
			.WithMany(t => t.Classrooms);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Schedules)
			.WithOne(s => s.Classroom)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Classroom>()
			.HasMany(c => c.Students)
			.WithOne(s => s.Classroom)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Calendar>()
			.HasMany(c => c.Days)
			.WithOne()
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<CalendarDay>()
			.HasMany(d => d.Lessons)
			.WithOne()
			.OnDelete(DeleteBehavior.Cascade);

		// schedule
		modelBuilder.Entity<Schedule>()
			.HasMany(s => s.ExamSlots)
			.WithOne(e => e.Schedule)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ExamSlot>()
			.HasMany(e => e.Participants)
			.WithMany(s => s.ParticipatingExamSlots);
		modelBuilder.Entity<ExamSlot>()
			.HasMany(e => e.ActuallyParticipated)
			.WithMany(s => s.ActuallyParticipatedExamSlots);
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
