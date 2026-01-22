using Entities;

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
			Role = entity.Role,
			SchoolId = entity.SchoolId,
		};
	}

	public static Models.API.UserProfile ToDTO(this StudentProfile entity)
	{
		return entity.UserProfile.ToDTO();
	}

	public static Models.API.TeacherProfile ToDTO(this TeacherProfile entity)
	{
		return new()
		{
			UserProfile = entity?.UserProfile.ToDTO(),
			CalendarTeacher = entity?.Teacher?.ToDTO(),
			Classrooms = entity?.Classrooms?.Select(x => x.ToDTO()) ?? [ ],
			Subjects = entity?.Teacher?.Subjects?.Select(x => x.ToDTO()) ?? [ ],
		};
	}
}
