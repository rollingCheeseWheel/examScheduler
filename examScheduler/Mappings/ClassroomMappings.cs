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
			SchoolId = entity.School.Id,
			CalendarId = entity.Calendar?.Id,
			Calendar = entity.Calendar?.ToDTO()
		};
	}

	public static Models.API.TeacherProfileClassroom ToTeacherProfileClassroomDTO(this Classroom entity)
	{
		return new()
		{
			Id = entity.Id,
			Name = entity.Name,
			SchoolId = entity.SchoolId,
			CalendarId = entity.Calendar?.Id
		};
	}
}
