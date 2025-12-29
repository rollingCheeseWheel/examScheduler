using Entities;
using examScheduler.Data;

namespace examScheduler.Mappings;

public static class UserProfileMappings
{
	public static Models.API.UserProfile ToDTO(this UserProfile entity)
	{
		return new()
		{
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Id = entity.Id,
			Role = entity.Role
		};
	}

	public static Models.API.UserProfile ToDTO(this StudentProfile entity) => entity.UserProfile.ToDTO();

	public static Models.API.TeacherProfile ToDTO(this TeacherProfile entity)
	{
		return new()
		{
			CalendarTeacher = entity?.Teacher?.ToDTO(),
			Classrooms = entity?.Teacher?.Classrooms?.Select(ClassroomMappings.ToDTO) ?? [ ],
			Subjects = entity?.Teacher?.Subjects?.Select(CalendarMappings.ToDTO) ?? [ ],
			UserProfile = entity?.UserProfile.ToDTO()
		};
	}
}
