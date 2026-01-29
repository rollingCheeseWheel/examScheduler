using Models.API;

namespace examScheduler.Mappings;

public static class SwapRequestMappings
{
	public static SwapRequest ToDTO(this Entities.SwapRequest entity) => new()
	{
		Id = entity.Id,
		RequestingStudentName = entity.RequestingStudentName,
		RequestingStudentId = entity.RequestingStudentId,
		RequestedSlotId = entity.RequestedSlotId,
	};
}	
