using Entities;

namespace examScheduler.Mappings;

public static class ScheduleMappings
{
    public static Models.API.Schedule ToDTO(this Schedule entity)
    {
        return new()
        {
            Id = entity.Id,
            Description = entity.Description,
            AutoLockIn = entity.AutoLockIn,
            LockInOffset = DateTimeOffset.UnixEpoch + entity.LockInOffset,
            FirstExamination = entity.FirstExamination,
            Subject = entity.Subject.ToDTO(),
            ExamSlots = entity.ExamSlots.Select(ToDTO),
            AuditLogs = entity.AuditLogs.Select(x => x.ToDTO()),
            SwapRequests = entity.SwapRequests.Select(x => x.ToDTO()),
        };
    }

    public static Models.API.ExamSlot ToDTO(this ExamSlot entity)
    {
        return new()
        {
            Id = entity.Id,
            Date = entity.Date,
            MinParticipants = entity.MinParticipants,
            MaxParticipants = entity.MaxParticipants,
            ActuallyParticipated = entity.ActuallyParticipated.Select(x => x.ToDTO()),
            Participants = entity.Participants.Select(x => x.ToDTO())
        };
    }
}
