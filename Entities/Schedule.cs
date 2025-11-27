using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Schedule
{
	[Key]
	public Guid Id { get; set; }

	[Required]
	public required DateTimeOffset FirstExamination { get; init; }
	[Required]
	public required AutoLockIn AutoLockIn { get; init; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = FirstExamination - LockInDate
	// AutoLockIn.TimeBeforeExamination = Offset, slot locks at this offset before the examination 
	[Required]
	public required TimeSpan LockInOffset { get; init; } = TimeSpan.Zero; // lock-in on examination
	[Required]
	public required string Description { get; init; }

	// Navigation properties
	[Required]
	public required ICollection<ScheduleGeneratorSlot> GeneratorSlots { get; init; }
	[Required]
	public required Subject Subject { get; init; }
	[Required]
	public required Classroom Classroom { get; init; }
	[Required]
	public ICollection<ExamSlot> ExamSlots { get; private set; } = [ ];
	[Required]
	public ICollection<AuditLog> AuditLogs { get; private set; } = [ ];

	public static bool operator ==(Schedule? a, Schedule? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.FirstExamination == b.FirstExamination
			&& a.Subject == b.Subject
			&& a.Classroom == b.Classroom;
	}
	public static bool operator !=(Schedule? a, Schedule? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Schedule other && this == other;
	public override int GetHashCode() => HashCode.Combine(FirstExamination, Subject, Classroom);
}

public class ExamSlot
{
	[Key]
	public Guid Id { get; set; }
	[Required]
	public required ScheduleGeneratorSlot GeneratorSlot { get; init; }
	/*[Required]
	public required int SlotIndex { get; init; }*/
	[Required]
	public required DateTimeOffset Date { get; init; }

	// Navigation Properties
	[Required]
	public required Schedule Schedule { get; init; }
	[Required]
	public ICollection<StudentProfile> Participants { get; private set; } = [ ];
	[Required]
	public ICollection<StudentProfile> ActuallyParticipated { get; private set; } = [ ];

	[NotMapped]
	public int RequiredParticipants { get => GeneratorSlot.RequiredParticipants; }
	[NotMapped]
	public int MaxParticipants { get => GeneratorSlot.MaxParticipants; }
	[NotMapped]
	public bool HasAlreadyHappened { get => Date < DateTimeOffset.UtcNow; }
	[NotMapped]
	public bool IsLocked
	{
		get => Schedule.AutoLockIn switch
		{
			AutoLockIn.FixedDate => Date >= ( Schedule.FirstExamination - Schedule.LockInOffset ),
			AutoLockIn.TimeBeforeExamination => DateTimeOffset.UtcNow >= ( Date - Schedule.LockInOffset ),
			_ => true,
		};
	}

	public static bool operator ==(ExamSlot? a, ExamSlot? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Schedule == b.Schedule
			&& a.Date == b.Date;
	}
	public static bool operator !=(ExamSlot? a, ExamSlot? b) => !( a == b );
	public override bool Equals(object? obj) => obj is ExamSlot other && this == other;
	public override int GetHashCode() => HashCode.Combine(Schedule);
}

public class ScheduleGeneratorSlot
{
	[Key]
	public Guid Id { get; set; }

	[Required, Range(0, int.MaxValue)]
	public required int Offset { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MaxParticipants { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int RequiredParticipants { get; set; }

	public static bool operator ==(ScheduleGeneratorSlot? a, ScheduleGeneratorSlot? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Offset == b.Offset
			&& a.MaxParticipants == b.MaxParticipants
			&& a.RequiredParticipants == b.RequiredParticipants;
	}
	public static bool operator !=(ScheduleGeneratorSlot? a, ScheduleGeneratorSlot? b) => !( a == b );
	public override bool Equals(object? obj) => obj is ScheduleGeneratorSlot other && this == other;
	public override int GetHashCode() => HashCode.Combine(Offset, MaxParticipants, RequiredParticipants);
}