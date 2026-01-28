using Entities;

namespace examScheduler.Mappings;

public static class CalendarMappings
{
	public static Models.API.Calendar ToDTO(this Calendar entity) => new()
	{
		Id = entity.Id,
		LastsUntil = entity.LastsUntil,
		Lessons = entity.Lessons.Select(ToDTO),
	};

	public static Models.API.Lesson ToDTO(this Lesson entity) => new()
	{
		Id = entity.Id,
		FromHour = entity.FromHour,
		ToHour = entity.ToHour,
		LessonName = entity.Name,
		Occurances = entity.Occurances,
		Subject = entity.Subject.ToDTO(),
		Teachers = entity.Teachers.Select(ToDTO)
	};

	public static Models.API.Teacher ToDTO(this Teacher entity) => new() { Name = entity.Name };

	public static Models.API.Subject ToDTO(this Subject entity) => new() { Name = entity.Name };
}
