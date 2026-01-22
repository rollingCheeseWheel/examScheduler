using System.ComponentModel.DataAnnotations;
using Util;

namespace Models.API;

public class Schedule
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required AutoLockIn AutoLockIn { get; set; }
	[Required]
	public required DateTimeOffset FirstExamination { get; set; }
	[Required]
	public required DateTimeOffset LockInOffset { get; set; }
	[Required]
	public required string Description { get; set; }
	[Required]
	public required Subject Subject { get; set; }
	[Required]
	public required IEnumerable<ExamSlot> ExamSlots { get; set; }
	[Required]
	public required IEnumerable<AuditLog> AuditLogs { get; set; }
	[Required]
	public required IEnumerable<SwapRequest> SwapRequests { get; set; }
}

public class ExamSlot
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required DateTimeOffset Date { get; set; }
	[Required]
	public required IEnumerable<UserProfile> Participants { get; set; }
	[Required]
	public required IEnumerable<UserProfile> ActuallyParticipated { get; set; }
	[Required]
	public required int MaxParticipants { get; set; }
	[Required]
	public required int MinParticipants { get; set; }
}

public class ScheduleGeneratorSlot
{
	[Required]
	public required int Offset { get; set; }
	[Required]
	public required int MinParticipants { get; set; }
	[Required]
	public required int MaxParticipants { get; set; }
}