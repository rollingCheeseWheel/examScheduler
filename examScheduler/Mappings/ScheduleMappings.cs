using Entities;

namespace examScheduler.Mappings;

public static class ScheduleMappings
{
	public static Models.API.Schedule ToDTO(this Schedule entity) => new()
	{
		Id = entity.Id,
		Description = entity.Description,
		AutoLockIn = entity.AutoLockIn,
		LockInOffset = entity.AutoLockInOffset,
		StartDate = entity.StartDate,
		EndDate = entity.EndDate,
		Subject = entity.Subject.ToDTO(),
		ExamSlots = entity.ExamSlots.Select(ToDTO),
		AuditLogs = entity.AuditLogs.Select(x => x.ToDTO()),
		SwapRequests = entity.SwapRequests.Select(x => x.ToDTO()),
		Teachers = entity.Teachers.Select(x => x.ToDTO()),
	};

	public static Models.API.ExamSlot ToDTO(this ExamSlot entity) => new()
	{
		Id = entity.Id,
		Date = entity.Date,
		MinParticipants = entity.MinParticipants,
		MaxParticipants = entity.MaxParticipants,
		Participants = entity.Participants.Select(x => x.ToDTO()),
		IsLocked = entity.IsLocked,
	};

	public static Models.API.ScheduleGeneratorSlot ToDTO(this ScheduleGeneratorSlot entity) => new()
	{
		MaxParticipants = entity.MaxParticipants,
		MinParticipants = entity.MinParticipants,
		Offset = entity.Offset,
	};
}
