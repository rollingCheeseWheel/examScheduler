using Models.API;

namespace examScheduler.Mappings;

public static class SwapRequestMappings
{
	public static SwapRequest ToDTO(this Entities.SwapRequest entity)
	{
		return new()
		{
			Id = entity.Id,
			ScheduleId = entity.ScheduleId,
			RequestingStudentName = entity.RequestingStudentName,
			RequestedStudentName = entity.RequestedStudentName,
			RequestedStudentId = entity.RequestedStudentId,
			RequestingStudentId = entity.RequestedStudentId,
			ExpirationDate = entity.ExpirationDate
		};
	}
}
