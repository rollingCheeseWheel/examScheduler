using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Schedule : EntityBase<Schedule>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
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

    public bool TryEnlistStudent(Guid examslotId, StudentProfile student)
    {
        var slot = ExamSlots.FirstOrDefault(s => s.Id == examslotId);
        return slot?.TryEnlistStudent(student) ?? false;
    }

    public override bool EqualsCore(Schedule b) =>
        FirstExamination == b.FirstExamination &&
        Subject == b.Subject &&
        Classroom == b.Classroom;
    public override int GetHashCode() => HashCode.Combine(FirstExamination, Subject, Classroom);
    public override int CompareTo(Schedule? other) => FirstExamination.CompareTo(other?.FirstExamination ?? DateTimeOffset.MinValue);
}

public class ExamSlot : EntityBase<ExamSlot>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
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
        GeneratorSlot == b.GeneratorSlot;
    public override int GetHashCode() => HashCode.Combine(Schedule, Date, GeneratorSlot);
    public override int CompareTo(ExamSlot? other) => Date.CompareTo(other?.Date ?? DateTimeOffset.MinValue);
}

public class ScheduleGeneratorSlot : EntityBase<ScheduleGeneratorSlot>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
    [Required, Range(0, int.MaxValue)]
    public required int Offset { get; set; }
    [Required, Range(0, int.MaxValue)]
    public required int MaxParticipants { get; set; }
    [Required, Range(0, int.MaxValue)]
    public required int MinParticipants { get; set; }

    public override bool EqualsCore(ScheduleGeneratorSlot b) =>
        Offset == b.Offset &&
        MaxParticipants == b.MaxParticipants &&
        MinParticipants == b.MinParticipants;
    public override int GetHashCode() => HashCode.Combine(Offset, MaxParticipants, MinParticipants);
    public override int CompareTo(ScheduleGeneratorSlot? other)
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