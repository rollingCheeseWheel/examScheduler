using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Schedule : IComparable<Schedule>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();

	[Required]
	public required DateTimeOffset FirstExamination { get; init; }
	[Required]
	public required AutoLockIn AutoLockIn { get; init; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = FirstExamination - LockInDate
	// AutoLockIn.TimeBeforeExamination = Offset, slot locks at this offset before the examination 
	[Required]
	public required TimeSpan LockInOffset { get; init; } = TimeSpan.Zero; // offset into the past from the date of the examination
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

	public bool TrySwapStudents(StudentProfile firstStudent, StudentProfile secondStudent)
	{
		var firstStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(firstStudent));
		var secondStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(secondStudent));
		if (firstStudentExamSlot is null || 
			secondStudentExamSlot is null || 
			firstStudentExamSlot.Id == secondStudentExamSlot.Id
		)
		{
			return false;
		}

		if (!firstStudentExamSlot.TrySwapStudents(firstStudent, secondStudent) ||
			!secondStudentExamSlot.TrySwapStudents(secondStudent, firstStudent)
		)
		{
			return false;
		}
		return true;
	}

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
	public int CompareTo(Schedule? other)
	{
		if (other is null) { return 1; }
		var res = FirstExamination.CompareTo(other.FirstExamination);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}
}

public class ExamSlot : IComparable<ExamSlot>
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
	public int MinParticipants { get => GeneratorSlot.MinParticipants; }
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

	internal bool TrySwapStudents(StudentProfile replaced, StudentProfile replacement)
	{
		if (IsLocked)
		{
			return false;
		}

		if (!Participants.Contains(replaced))
		{
			return false;
		}

		Participants.Remove(replaced);
		Participants.Add(replacement);
		return true;
	}

	public static bool operator ==(ExamSlot? a, ExamSlot? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Schedule == b.Schedule
			&& a.Date == b.Date
			&& a.GeneratorSlot == b.GeneratorSlot;
	}
	public static bool operator !=(ExamSlot? a, ExamSlot? b) => !( a == b );
	public override bool Equals(object? obj) => obj is ExamSlot other && this == other;
	public override int GetHashCode() => HashCode.Combine(Schedule);
	public int CompareTo(ExamSlot? other)
	{
		if (other is null) { return 1; }
		var res = Date.CompareTo(other.Date);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}
}

public class ScheduleGeneratorSlot : IComparable<ScheduleGeneratorSlot>
{
	[Key]
	public Guid Id { get; set; }

	[Required, Range(0, int.MaxValue)]
	public required int Offset { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MaxParticipants { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MinParticipants { get; set; }

	public static bool operator ==(ScheduleGeneratorSlot? a, ScheduleGeneratorSlot? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Offset == b.Offset
			&& a.MaxParticipants == b.MaxParticipants
			&& a.MinParticipants == b.MinParticipants;
	}
	public static bool operator !=(ScheduleGeneratorSlot? a, ScheduleGeneratorSlot? b) => !( a == b );
	public override bool Equals(object? obj) => obj is ScheduleGeneratorSlot other && this == other;
	public override int GetHashCode() => HashCode.Combine(Offset, MaxParticipants, MinParticipants);
	public int CompareTo(ScheduleGeneratorSlot? other)
	{
		if (other is null) { return 1; }
		var res = Offset.CompareTo(other.Offset);
		if (res != 0) { return res; }
		res = MinParticipants.CompareTo(other.MinParticipants);
		if (res != 0) { return res; }
		res = MaxParticipants.CompareTo(other.MaxParticipants);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}
}