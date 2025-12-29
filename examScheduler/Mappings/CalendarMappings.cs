using Entities;

namespace examScheduler.Mappings;

public static class CalendarMappings
{
	public static Models.API.Calendar ToDTO(this Calendar entity)
	{
		return new()
		{
			Id = entity.Id,
			LastsUntil = entity.LastsUntil,
			Lessons = entity.Lessons.Select(ToDTO)
		};
	}

	public static Models.API.Lesson ToDTO(this Lesson entity)
	{
		return new()
		{
			Id = entity.Id,
			FromHour = entity.FromHour,
			ToHour = entity.ToHour,
			LessonName = entity.LessonName,
			Occurances = entity.Occurances,
			Subject = entity.Subject.ToDTO(),
			Teachers = entity.Teachers.Select(ToDTO)
		};
	}

	public static Models.API.Subject ToDTO(this Subject entity)
	{
		return new() { Name = entity.Name };
	}

	public static Models.API.Teacher ToDTO(this Teacher entity)
	{
		return new() { FirstName = entity.FirstName, LastName = entity.LastName };
	}
}
