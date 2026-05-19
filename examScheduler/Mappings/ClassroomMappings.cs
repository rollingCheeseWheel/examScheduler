using Entities;

namespace examScheduler.Mappings;

public static class ClassroomMappings
{
	public static Models.API.Classroom ToDTO(this Classroom entity) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Teachers = entity.Teachers.Select(x => x.ToTeacherWithSubjectsDTO())
	};
}
