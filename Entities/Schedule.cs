using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Numerics;
using Util;

namespace Entities;

public class Schedule
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required]
	public required DateTime FirstExamination { get; set; }
	[Required]
	public required AutoLockIn AutoLockIn { get; set; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = FirstExamination - LockInDate
	// AutoLockIn.TimeBeforeExamination = Offset, slot locks at this offset before the examination 
	[Required]
	public TimeSpan LockInDate { get; set; } = TimeSpan.Zero; // lock-in on examination

	[Required]
	public required ScheduleGenerator SlotRule { get; set; }
	[Required]
	public required Subject Subject { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }

	[NotMapped]
	public int RequiredParticipants { get => ExamSlots.Select(e => e.RequiredParticipants).Sum(); }
	[NotMapped]
	public int Participants { get => ExamSlots.Select(e => e.Participating).Sum(); }

	// Navigation Properties
	[Required]
	public ICollection<ExamSlot> ExamSlots { get; private set; } = [ ];
	[NotMapped]
	public IEnumerable<Teacher> Teachers { get => Classroom.Teachers.Where(t => t.Subjects.Contains(Subject)); }

	public bool TrySwapStudents(Student student1, Student student2)
	{
		if (ExamSlots.Count == 0)
		{
			return false;
		}
		if (student1 == student2)
		{
			return true;
		}

		var slot1 = GetExamSlot(student1);
		var slot2 = GetExamSlot(student2);
		if (slot1 is null || slot2 is null)
		{
			return false;
		}
		else if (slot1.AlreadyHappened || slot2.AlreadyHappened)
		{
			return false;
		}
		else
		{
			return slot1.TrySwapStudents(student1, student2)
				&& slot2.TrySwapStudents(student2, student1);
		}
	}

	public ExamSlot? GetExamSlot(Student student) => ExamSlots.Where(e => e.IsParticipating(student)).FirstOrDefault();

	public bool TryEnlistStudent(Student student, DateTime date)
	{
		throw new NotImplementedException();
	}

	public bool TryEnlistStudent(Student student, ExamSlot slot)
	{
		throw new NotImplementedException();
	}

	public bool TryEnlistStudentAtNearestDate(Student student)
	{
		if (GetExamSlot(student) is not null) // student is already enlisted 
		{
			return true;
		}
		var nextOpenExamSlot = GetNextOpenExamSlot();
		if (nextOpenExamSlot is null)
		{
			return false;
		}

		return nextOpenExamSlot.TryEnlistStudent(student);
	}

	public bool EnlistStudentAtNearesDateForcefully(Student student)
	{

		throw new NotImplementedException();
	}

	public ExamSlot? GetNextOpenExamSlot()
	{
		return ExamSlots
			.Where(e => !e.IsFull())
			.OrderBy(e => e.Participating)
			.ThenBy(e => e.Date)
			.ThenByDescending(e => e.RequiredParticipants)
			.FirstOrDefault();
	}

	[Obsolete]
	public ExamSlot? GetNextOpenExamSlotForcefully()
	{
		return ExamSlots
			.Select((e, index) => new { e, index })
			.OrderBy(a => a.index)
			.Select(a => a.e)
			.Where(e => !e.IsFull())
			.OrderBy(e => e.Participating)
			.ThenBy(e => e.Date)
			.ThenByDescending(e => e.RequiredParticipants)
			.FirstOrDefault();
	}
}

public class ExamSlot
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required]
	[Range(0, int.MaxValue)]
	public required int RequiredParticipants { get; set; }
	[Required]
	[Range(0, int.MaxValue)]
	public required int MaxParticipants { get; set; }
	[Required]
	public required DateTime Date { get; set; }
	[NotMapped]
	public bool IsLocked { get; set; }

	// Navigation Properties
	[Required]
	public ICollection<Student> Participants { get; private set; } = [ ];
	[Required]
	public ICollection<Student> ActuallyParticipated { get; private set; } = [ ];
	[Required]
	public required Schedule Schedule { get; set; }
	[Required]
	public required ScheduleGeneratorSlot GeneratorSlot { get; set; }

	[NotMapped]
	public bool AlreadyHappened { get => Date <= DateTime.UtcNow; }
	[NotMapped]
	public int Participating { get => Participants.Count; }

	internal bool TrySwapStudents(Student target, Student replacement)
	{
		if (!IsParticipating(target))
		{
			return false;
		}
		else if (!Participants.Remove(target))
		{
			return false;
		}
		else
		{
			Participants.Add(replacement);
			return true;
		}
	}

	public bool IsParticipating(Student student) => Participants.Contains(student);

	public bool TryEnlistStudent(Student student)
	{
		if (IsParticipating(student))
		{
			return false;
		}
		else if (IsFull())
		{
			return false;
		}
		else
		{
			Participants.Add(student);
			return true;
		}
	}

	public bool IsFull() => Participating >= MaxParticipants;

	public int GetMissingParticipantCount() => Math.Max(Participating - RequiredParticipants, 0);
}

public class ScheduleGenerator(ScheduleGeneratorBehavior behavior, ICollection<ScheduleGeneratorSlot> slots, Schedule schedule)
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required]
	public required ScheduleGeneratorBehavior Behavior { get; set; } = behavior;
	[Required]
	public ICollection<ScheduleGeneratorSlot> Slots { get; private set; } = slots;

	[Required]
	public required Schedule Schedule { get; set; } = schedule;

	public ICollection<ExamSlot>? Expand(int studentCount, ICollection<ExamSlot> slots)
	{
		if (Behavior != ScheduleGeneratorBehavior.Generator ||
			Slots.Count == 0 ||
			!DoSlotsMatch(slots)
		)
		{
			return null;
		}

		var startDate = Schedule.FirstExamination;

		var actual = Slots.Max(s => s.Offset).RoundUpToMultiple(7); // makes sure that slots dont overlap, slots are tiled weekly

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
			result.Add(TranslateSingleSlot(GetSlotAtWrapAround(i)));
		}

		return result;
	}

	public ICollection<ExamSlot>? Generate(int studentCount)
	{
		if (Slots.Count == 0) // nothing to be generated
		{
			return null;
		}

		return Behavior switch
		{
			ScheduleGeneratorBehavior.Generator => GenerateUsingRule(studentCount),
			ScheduleGeneratorBehavior.ListOfAvailableSlots => GenerateUsingListOfAvailableSlots(studentCount),
			_ => null,
		};
	}

	private ExamSlot TranslateSingleSlot(ScheduleGeneratorSlot slot, int additionalOffset = 0)
	{
		return new ExamSlot
		{
			Schedule = Schedule,
			Date = Schedule.FirstExamination.AddDays(slot.Offset + additionalOffset),
			MaxParticipants = slot.MaxParticipants,
			RequiredParticipants = slot.RequiredParticipants,
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

	private ICollection<ExamSlot> GenerateUsingRule(int studentCount)
	{
		var result = TranslateSlots();
		if (result.Sum(s => s.MaxParticipants) > studentCount)
		{
			return result; // no further work needed
		}

		return Expand(studentCount, result)!;
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