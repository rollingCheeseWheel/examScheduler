using Entities;

namespace examScheduler.Mappings;

public static class ClassroomMappings
{
	public static Models.API.Classroom ToDTO(this Classroom entity)
	{
		return new()
		{
			Id = entity.Id,
			Name = entity.Name,
			School = entity.School.ToDTO(),
			Calendar = entity.Calendar?.ToDTO()
		};
	}
}
