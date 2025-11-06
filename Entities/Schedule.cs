using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Schedule
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

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
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
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

[Obsolete("New logic in place")]
public class ScheduleGenerator
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required]
	public required ICollection<ScheduleGeneratorSlot> Slots { get; init; }
	[Required]
	public required Schedule Schedule { get; init; }

	public ICollection<ExamSlot>? Expand(int studentCount, ICollection<ExamSlot> slots)
	{
		var startDate = Schedule.FirstExamination;

		var totalOffset = Slots.Max(s => s.Offset).RoundUpToMultiple(7); // makes sure that slots dont overlap, slots are tiled weekly

		var result = slots.ToList();

		var i = result.Count; // pick up the generation where it left of
		while (result.Select(s =>
		{
			if (s.ActuallyParticipated.Count == 0)
			{
				return s.MaxParticipants;
			}
			else
			{
				return s.ActuallyParticipated.Count;
			}
		}).Sum() <= studentCount) // generate further until all the students are accounted for
		{
			result.Add(TranslateSingleSlot(GetSlotAtWrapAround(i), (int)( double.Floor(i / totalOffset) * totalOffset )));
		}

		return result;
	}

	public ICollection<ExamSlot>? Generate(int studentCount)
	{
		if (Slots.Count == 0) // nothing to be generated
		{
			return null;
		}

		var result = TranslateSlots();
		if (result.Sum(s => s.MaxParticipants) > studentCount)
		{
			return result; // no further work needed
		}

		return Expand(studentCount, result)!;
	}

	private ExamSlot TranslateSingleSlot(ScheduleGeneratorSlot slot, int additionalOffset = 0)
	{
		return new ExamSlot
		{
			Schedule = Schedule,
			Date = Schedule.FirstExamination.AddDays(slot.Offset + additionalOffset),/*
			MaxParticipants = slot.MaxParticipants,
			RequiredParticipants = slot.RequiredParticipants,*/
			GeneratorSlot = slot,
		};
	}

	private ICollection<ExamSlot> TranslateSlots() => [ .. Slots.Select(TranslateSingleSlot) ];

	private ICollection<ExamSlot>? GenerateUsingListOfAvailableSlots(int studentCount)
	{
		if (Slots.Sum(s => s.MaxParticipants) > studentCount)
		{
			return null;
		}
		else
		{
			return TranslateSlots();
		}
	}

	public ScheduleGeneratorSlot GetSlotAtWrapAround(int index) => Slots.ElementAt(( index % Slots.Count + Slots.Count ) % Slots.Count);

	public bool DoSlotsMatch(ICollection<ExamSlot> slots) => slots.Select((s, i) => s.GeneratorSlot == GetSlotAtWrapAround(i)).All(b => b);
}

public class ScheduleGeneratorSlot
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

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