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
	[Required, DefinedEnum]
	public required AutoLockIn AutoLockIn { get; set; }
	[Required, PositiveTimeSpan, JsonConverter(typeof(TimeSpanToDateTimeOffsetConverter))]
	public required TimeSpan LockInOffset { get; set; }
	public string? Description { get; set; }
	[Required]
	public required Subject Subject { get; set; }
	[Required]
	public required IEnumerable<Teacher> Teachers { get; set; }
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
	[Required]
	public required bool IsLocked { get; set; }
}

public class ScheduleCreateRequest
{
	[Required]
	public required Guid ClassroomId { get; set; }
	[Required]
	public required string SubjectName { get; set; }
	public string? Description { get; set; }
	[Required, DefinedEnum]
	public required SlotFillingBehaviour SlotFillingBehaviour { get; set; }
	[Required, DefinedEnum]
	public required AutoLockIn AutoLockIn { get; set; }
	[Required, PositiveDateTimeOffset]
	public required DateTimeOffset StartDate { get; set; }
	[Required, GreaterThan<DateTimeOffset>(nameof(StartDate))]
	public required DateTimeOffset EndDate { get; set; }
	[Required, PositiveTimeSpan, JsonConverter(typeof(TimeSpanToDateTimeOffsetConverter))]
	public required TimeSpan LockInOffset { get; set; }
	[Required]
	public required ScheduleGenerator Generator { get; set; }
}

public class ScheduleGenerator
{
	[Required, MaxLength(7)]
	public required IEnumerable<ScheduleGeneratorSlot> Slots { get; set; }
	[Required, MaxLength(20)]
	public required IEnumerable<DateTimeOffset> BlacklistedDays { get; set; }
}

public class ScheduleGeneratorSlot
{
	[Required, DefinedEnum]
	public required DayOfWeek DayOfWeek { get; set; }
	[Required, MinValue(0)]
	public required int MinParticipants { get; set; }
	[Required, GreaterThan<int>(nameof(MinParticipants))]
	public required int MaxParticipants { get; set; }
}