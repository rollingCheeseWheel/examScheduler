using Entities;

namespace examScheduler.Mappings;

public static class ScheduleMappings
{
	public static Models.API.Schedule ToDTO(this Schedule entity) => new()
	{
		Id = entity.Id,
		ClassroomId = entity.ClassroomId,
		Description = entity.Description,
		StartDate = entity.StartDate,
		EndDate = entity.EndDate,
		Subject = entity.Subject.ToDTO(),
		ExamSlots = entity.ExamSlots.Select(ToDTO),
		AuditLogs = entity.AuditLogs.Select(x => x.ToDTO()),
		SwapRequests = entity.SwapRequests.Select(x => x.ToDTO()),
		Teachers = entity.Teachers.Select(x => x.ToTeacherWithSubjectsDTO()),
	};

	public static Models.API.ExamSlot ToDTO(this ExamSlot entity) => new()
	{
		Id = entity.Id,
		Date = entity.Date,
		LockInDate = entity.LockInDate,
		MaxParticipants = entity.MaxParticipants,
		Participants = entity.Participants.Select(x => x.ToDTO()),
		LockState = entity.IsTeacherConfirmed ? Util.SlotLockState.Definite : entity.IsLocked ? Util.SlotLockState.Locked : Util.SlotLockState.Open,
	};

	public static ScheduleGeneratorSlot ToEntity(this Models.API.ScheduleGeneratorSlot dto) => new()
	{
		DayOfWeek = dto.DayOfWeek,
		MaxParticipants = dto.MaxParticipants,
	};
}
