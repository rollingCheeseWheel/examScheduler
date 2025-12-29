using Entities;

namespace examScheduler.Mappings;

public static class SchoolMappings
{
	public static Models.API.School ToDTO(this School entity)
	{
		return new() { Name = entity.Name, ClientId = entity.ClientId, RegisterUri = entity.RegisterUri };
	}
}
