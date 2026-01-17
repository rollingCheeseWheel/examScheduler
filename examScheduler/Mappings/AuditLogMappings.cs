using Entities;

namespace examScheduler.Mappings;

public static class AuditLogMappings
{
    public static Models.API.AuditLog ToDTO(this AuditLog entity)
    {
        return new()
        {
            Action = entity.Action,
            Actor = entity.Actor,
            ActorType = entity.ActorType,
            ActorName = entity.ActorName,
            Description = entity.Description,
            Timestamp = entity.Timestamp,
        };
    }
}
