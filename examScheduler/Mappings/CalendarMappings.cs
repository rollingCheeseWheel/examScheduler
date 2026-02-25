using Entities;
using Util.Extensions;

namespace examScheduler.Mappings;

public static class CalendarMappings
{
	public static Models.API.Calendar ToDTO(this Calendar entity) => new()
	{
		Id = entity.Id,
		LastsUntil = entity.LastsUntil,
		Actual = entity
			.NormalizeOrDefaultToMostCommonLesson_CreatesNewInstances(DateTimeOffset.UtcNow.RoundDownToMonday())
			.Select(x => x.Select(ToDTO)),
		Fallback = entity.NormalizeToSingleWeek().Select(ToDTO),
	};

	public static Models.API.Lesson ToDTO(this Lesson entity) => new()
	{
		Id = entity.Id,
		FromHour = entity.FromHour,
		ToHour = entity.ToHour,
		LessonName = entity.Name,
		Occurances = entity.Occurances,
		DayOfWeek = entity.FirstOccurance?.DayOfWeek ?? (DayOfWeek)( -1 ),
		Subject = entity.Subject.ToDTO(),
		Teachers = entity.Teachers.Select(ToDTO)
	};

	public static Models.API.Teacher ToDTO(this Teacher entity) => new(entity.Name);

	public static Models.API.Subject ToDTO(this Subject entity) => new(entity.Name);
}
