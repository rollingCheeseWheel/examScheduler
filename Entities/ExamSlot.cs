using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public class ExamSlot : EntityBase<ExamSlot>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();
	[Required]
	public required Guid ScheduleId { get; set; }
	[Required]
	public required DateOnly Date { get; set; }
	[Required]
	public required DateTimeOffset LockInDate { get; set; }

	[NotMapped]
	public bool IsLocked => LockInDate <= DateTimeOffset.UtcNow;
	[NotMapped]
	public bool ShouldBeFilled => IsLocked && Date >= DateTimeOffset.UtcNow.ToDateOnly() && !HasBeenProcessed;
	[NotMapped]
	public bool CanTeacherReportStudents => IsLocked && Date <= DateTimeOffset.UtcNow.ToDateOnly();

	[Required, EditorBrowsable(EditorBrowsableState.Never)]
	public bool HasBeenProcessed { get; set; } = false;

	[Required]
	public bool IsGenerated { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MinParticipants { get; set; }
	[Required, Range(0, int.MaxValue), GreaterThan<int>(nameof(MinParticipants))]
	public required int MaxParticipants { get; set; }

	[Required]
	public ICollection<StudentProfile> Participants { get; private set; } = [ ];
	[Required]
	public bool IsTeacherConfirmed { get; set; } = false;

	[Timestamp]
	public override uint Version { get; set; }

	internal bool TryReportStudents(IEnumerable<StudentProfile> students, out IEnumerable<StudentProfile> previousStudents)
	{
		previousStudents = [ ];

		if (!CanTeacherReportStudents)
		{
			return false;
		}

		previousStudents = [ .. Participants ];
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
		ScheduleId == b.ScheduleId &&
		Date == b.Date &&
		Participants.ValueEquals(b.Participants) &&
		MinParticipants == b.MinParticipants &&
		MaxParticipants == b.MaxParticipants;

	public override int GetHashCode() => HashCode.Combine(ScheduleId, Date, Participants.GetValueHashCode(), MinParticipants, MaxParticipants);

	public override int CompareTo(ExamSlot? other) => Date.CompareTo(other?.Date ?? default);
}
