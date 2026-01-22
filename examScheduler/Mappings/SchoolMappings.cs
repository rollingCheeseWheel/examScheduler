using Entities;

namespace examScheduler.Mappings;

public static class SchoolMappings
{
	public static Models.API.School ToDTO(this School entity) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		ClientId = entity.ClientId,
		RegisterUri = entity.RegisterUri,
		IsEnabled = entity.IsEnabled
	};
}
