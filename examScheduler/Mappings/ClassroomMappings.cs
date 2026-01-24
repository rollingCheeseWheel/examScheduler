using Entities;

namespace examScheduler.Mappings;

public static class ClassroomMappings
{
	public static Models.API.Classroom ToDTO(this Classroom entity) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		StudentCount = entity.Students.Count,
		SchoolId = entity.SchoolId,
		CalendarId = entity.Calendar?.Id,
		Calendar = entity.Calendar?.ToDTO(),
	};
}
