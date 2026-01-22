using Entities;

namespace examScheduler.Mappings;

public static class UserMappings
{
	public static Models.API.UserProfile ToDTO(this UserProfile entity)
	{
		return new()
		{
			Id = entity.Id,
			Name = entity.Name,
			Role = entity.Role,
		};
	}

	public static Models.API.UserProfile ToDTO(this StudentProfile entity)
	{
		return entity.UserProfile.ToDTO();
	}

	public static Models.API.UserProfile ToDTO(this TeacherProfile entity)
	{
		return entity.UserProfile.ToDTO();
	}
}
