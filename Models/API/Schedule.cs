using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util;
using Util.Converters;
using Util.Validation;

namespace Models.API;

public class Schedule
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required DateTimeOffset StartDate { get; set; }
	[Required, GreaterThan<DateTimeOffset>(nameof(StartDate))]
	public required DateTimeOffset EndDate { get; set; }
	[Required, ValidEnum]
	public required AutoLockIn AutoLockIn { get; set; }
	[Required, PositiveTimeSpan]
	public required TimeSpan LockInOffset { get; set; }
	public string? Description { get; set; }
	[Required]
	public required string SubjectName { get; set; }
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
	public required int MaxParticipants { get; set; }
	[Required]
	public required int MinParticipants { get; set; }
}

public class ScheduleCreateRequest
{
	[Required]
	public required Guid ClassroomId { get; set; }
	[Required, ValidEnum]
	public required SlotFillingBehaviour SlotFillingBehaviour { get; set; }
	[Required, ValidEnum]
	public required AutoLockIn AutoLockIn { get; set; }
	[Required]
	public required DateTimeOffset StartDate { get; set; }
	[Required]
	public required DateTimeOffset EndDate { get; set; }
	[Required, PositiveTimeSpan]
	public required TimeSpan LockInOffset { get; set; }
	public string? Description { get; set; }
	[Required]
	public required string SubjectName { get; set; }
	[Required]
	public required IEnumerable<ScheduleGeneratorSlot> GeneratorSlots { get; set; }
}

public class ScheduleGeneratorSlot
{
	[Required, PositiveTimeSpan]
	public required TimeSpan Offset { get; set; }
	[Required]
	public required int MinParticipants { get; set; }
	[Required]
	public required int MaxParticipants { get; set; }
}