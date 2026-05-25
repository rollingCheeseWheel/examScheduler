using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
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
	public bool IsLocked => LockInDate <= DateTimeOffset.UtcNow || IsTeacherConfirmed;
	[NotMapped]
	public bool ShouldBeFilled => IsLocked && !HasBeenAutoFilled;
	[Required, EditorBrowsable(EditorBrowsableState.Never)]
	public bool HasBeenAutoFilled { get; set; } = false;

	[Required]
	public bool IsTeacherConfirmed { get; set; } = false;
	[NotMapped]
	public bool CanTeacherReportStudents => IsLocked && Date <= DateTimeOffset.UtcNow.ToDateOnly();

	[Required]
	public bool IsGenerated { get; set; }
	[Required, MinValue(1)]
	public required int MaxParticipants { get; set; }

	[Required]
	public ICollection<StudentProfile> Participants { get; private set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	internal Models.API.Result TryReportStudents(IEnumerable<StudentProfile> students, out IEnumerable<StudentProfile> misplacedStudents)
	{
		misplacedStudents = [ ];

		if (!CanTeacherReportStudents)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		misplacedStudents = Participants.Except(students).ToList();
		Participants.Clear();
		Participants.AddRange(students);

		return new(HttpStatusCode.OK);
	}

	internal Models.API.Result TrySwapStudents(StudentProfile replaced, StudentProfile replacement)
	{
		if (IsLocked)
		{
			return new(HttpStatusCode.BadRequest, "Slot is locked");
		}

		if (!Participants.Remove(replaced))
		{
			return new(HttpStatusCode.BadRequest, "Student not present in participants");
		}
		Participants.Add(replacement);
		return new(HttpStatusCode.OK);
	}

	internal Models.API.Result TryEnlistStudent(StudentProfile student)
	{
		if (IsLocked)
		{
			return new(HttpStatusCode.BadRequest, "Slot is locked");
		}
		if (Participants.Contains(student))
		{
			return new(HttpStatusCode.BadRequest, "Student is already enlisted");
		}
		Participants.Add(student);
		return new(HttpStatusCode.OK);
	}

	public override bool EqualsCore(ExamSlot b) =>
		ScheduleId == b.ScheduleId &&
		Date == b.Date &&
		Participants.ValueEquals(b.Participants) &&
		MaxParticipants == b.MaxParticipants;

	public override int GetHashCode() => HashCode.Combine(ScheduleId, Date, Participants.GetValueHashCode(), MaxParticipants);

	public override int CompareTo(ExamSlot? other) => Date.CompareTo(other?.Date ?? default);
}
