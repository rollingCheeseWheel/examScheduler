using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class ExamSlot : EntityBase<ExamSlot>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required ScheduleGeneratorSlot GeneratorSlot { get; set; }
	/*[Required]
	public required int SlotIndex { get; init; }*/
	[Required]
	public required DateTimeOffset Date { get; set; }

	// Navigation Properties
	[Required]
	public required Schedule Schedule { get; set; }
	[Required]
	public Guid ScheduleId { get; private set; }
	[Required]
	public ICollection<StudentProfile> Participants { get; set; } = [ ];
	[Required]
	public ICollection<StudentProfile> ActuallyParticipated { get; set; } = [ ];

	[NotMapped]
	public int MinParticipants => GeneratorSlot.MinParticipants;
	[NotMapped]
	public int MaxParticipants => GeneratorSlot.MaxParticipants;
	[NotMapped]
	public bool HasAlreadyHappened => Date < DateTimeOffset.UtcNow;
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

	internal bool TryEnlistStudent(StudentProfile student)
	{
		if (IsLocked || Participants.Contains(student))
		{
			return false;
		}
		Participants.Add(student);
		return true;
	}

	public override bool EqualsCore(ExamSlot b) => Schedule == b.Schedule &&
		Date == b.Date &&
		GeneratorSlot == b.GeneratorSlot;

	public override int GetHashCode() => HashCode.Combine(Schedule, Date, GeneratorSlot);

	public override int CompareTo(ExamSlot? other) => Date.CompareTo(other?.Date ?? DateTimeOffset.MinValue);
}
