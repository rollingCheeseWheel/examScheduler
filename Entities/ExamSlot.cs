using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public class ExamSlot : EntityBase<ExamSlot>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required DateTimeOffset Date { get; set; }

	// Navigation Properties
	[Required]
	public Schedule Schedule { get; set; } = null!;
	[Required]
	public Guid ScheduleId { get; private set; }
	[Required]
	public ICollection<StudentProfile> Participants { get; private set; } = [ ];

	[Required, Range(0, int.MaxValue)]
	public required int MinParticipants { get; set; }
	[Required, Range(0, int.MaxValue), GreaterThan<int>(nameof(MinParticipants))]
	public required int MaxParticipants { get; set; }
	[NotMapped]
	public bool IsLocked
	{
		get => Schedule.AutoLockIn switch
		{
			AutoLockIn.FixedDate => Date >= ( Schedule.StartDate - Schedule.AutoLockInOffset ),
			AutoLockIn.TimeBeforeExamination => DateTimeOffset.UtcNow >= ( Date - Schedule.AutoLockInOffset ),
			_ => true,
		};
	}

	[Timestamp]
	public override uint Version { get; set; }

	internal bool TryReportStudents(IEnumerable<StudentProfile> students)
	{
		if (IsLocked)
		{
			return false;
		}

		Participants.Clear();
		Participants.AddRange(students);

		return true;
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

	internal bool TryEnlistStudent(StudentProfile student)
	{
		if (IsLocked || Participants.Contains(student))
		{
			return false;
		}
		Participants.Add(student);
		return true;
	}

	public override bool EqualsCore(ExamSlot b) =>
		Schedule == b.Schedule &&
		Date == b.Date &&
		Participants.ValueEquals(b.Participants) &&
		MinParticipants == b.MinParticipants &&
		MaxParticipants == b.MaxParticipants;

	public override int GetHashCode() => HashCode.Combine(Schedule, Date, Participants.Order(), MinParticipants, MaxParticipants);

	public override int CompareTo(ExamSlot? other) => Date.CompareTo(other?.Date ?? DateTimeOffset.MinValue);
}
