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
	public required DateOnly StartDate { get; set; }
	[Required, GreaterThan<DateOnly>(nameof(StartDate))]
	public required DateOnly EndDate { get; set; }
	//[Required, DefinedEnum, JsonConverter(typeof(EnumConverter<AutoLockIn>))]
	//public required AutoLockIn AutoLockIn { get; set; }
	//[Required, PositiveTimeSpan, JsonConverter(typeof(TimeSpanToDateTimeOffsetConverter))]
	//public required TimeSpan LockInOffset { get; set; }
	public string? Description { get; set; }
	[Required]
	public required Subject Subject { get; set; }
	[Required]
	public required IEnumerable<TeacherWithSubjects> Teachers { get; set; }
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
	public required DateOnly Date { get; set; }
	[Required]
	public required DateTimeOffset LockInDate { get; set; }
	[Required]
	public required IEnumerable<UserProfile> Participants { get; set; }
	[Required]
	public required int MaxParticipants { get; set; }
	[Required, DefinedEnum, JsonConverter(typeof(EnumConverter<SlotLockState>))]
	public required SlotLockState LockState { get; set; }
}

public class ScheduleCreateRequest
{
	[Required]
	public required Guid ClassroomId { get; set; }
	[Required]
	public required string SubjectName { get; set; }
	public string? Description { get; set; }
	//[Required, DefinedEnum, JsonConverter(typeof(EnumConverter<SlotFillingBehaviour>))]
	//public required SlotFillingBehaviour SlotFillingBehaviour { get; set; }
	//[Required, DefinedEnum, JsonConverter(typeof(EnumConverter<AutoLockIn>))]
	//public required AutoLockIn AutoLockIn { get; set; }
	[Required]
	public required DateOnly StartDate { get; set; }
	[Required, PositiveTimeSpan, JsonConverter(typeof(TimeSpanToDateTimeOffsetConverter))]
	public required TimeSpan LockInOffset { get; set; }
	[Required]
	public required ScheduleGenerator Generator { get; set; }
}

public class ScheduleGenerator
{
	[Required, DistinctBy<ScheduleGeneratorSlot>(nameof(ScheduleGeneratorSlot.DayOfWeek))]
	public required IEnumerable<ScheduleGeneratorSlot> Slots { get; set; }
	[Required, Distinct<DateOnly>, MaxLength(20)]
	public required IEnumerable<DateOnly> BlacklistedDays { get; set; }
}

public class ScheduleGeneratorSlot
{
	[Required, DefinedEnum, JsonConverter(typeof(EnumConverter<DayOfWeek>))]
	public required DayOfWeek DayOfWeek { get; set; }
	[Required, MinValue(1)]
	public required int MaxParticipants { get; set; }
}