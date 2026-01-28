using Entities;

namespace examScheduler.Mappings;

public static class AuditLogMappings
{
	public static Models.API.AuditLog ToDTO(this AuditLog entity) => new()
	{
		Timestamp = entity.Timestamp,
		Action = entity.Action,
		ActorType = entity.ActorType,
		FirstActorId = entity.FirstActorId,
		SecondActorId = entity.SecondActorId,
		FirstActorName = entity.FirstActorName,
		SecondActorName	= entity.SecondActorName,
		Description = entity.Description,
	};
}
