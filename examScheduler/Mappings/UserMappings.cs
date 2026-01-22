using Entities;

namespace examScheduler.Mappings;

public static class UserMappings
{
	public static Models.API.UserProfile ToDTO(this UserProfile entity) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Role = entity.Role,
	};

	public static Models.API.UserProfile ToDTO(this StudentProfile entity) => entity.UserProfile.ToDTO();

	public static Models.API.UserProfile ToDTO(this TeacherProfile entity) => entity.UserProfile.ToDTO();
}
