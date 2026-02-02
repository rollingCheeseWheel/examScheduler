using Entities;

namespace examScheduler.Mappings;

public static class AuditLogMappings
{
	public static Models.API.AuditLog ToDTO(this AuditLog entity) => new()
	{
		Timestamp = entity.Timestamp,
		Action = entity.Action,
		OriginType = entity.OriginType,
		OriginId = entity.OriginId,
		OriginName = entity.OriginName,
		TargetType = entity.TargetType,
		TargetId = entity.TargetId,
		TargetName = entity.TargetName,
		Description = entity.Description,
	};
}
