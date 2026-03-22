using Entities;
using Util.Extensions;

namespace examScheduler.Mappings;

public static class CalendarMappings
{
	//public static Models.API.Calendar ToTeacherWithSubjectsDTO(this Calendar entity) => new()
	//{
	//	Id = entity.Id,
	//	LastsUntil = entity.LastsUntil,
	//	Actual = entity
	//		.NormalizeOrDefaultToMostCommonLesson_CreatesNewInstances(DateTimeOffset.UtcNow.RoundDownToMonday())
	//		.Select(x => x.Select(ToTeacherWithSubjectsDTO)),
	//	Fallback = entity.NormalizeToSingleWeek().Select(ToTeacherWithSubjectsDTO),
	//};

	public static Models.API.Lesson ToDTO(this Lesson entity) => new()
	{
		Id = entity.Id,
		FromHour = entity.FromHour,
		ToHour = entity.ToHour,
		LessonName = entity.Name,
		Date = entity.FirstOccurance ?? DateTime.UnixEpoch.ToDateOnly(),
		Subject = entity.Subject.ToDTO(),
		Teachers = entity.Teachers.Select(ToTeacherOnlyDTO)
	};

	public static Models.API.TeacherOnly ToTeacherOnlyDTO(this Teacher entity) => new() { Name = entity.Name };

	public static Models.API.TeacherWithSubjects ToTeacherWithSubjectsDTO(this Teacher entity) => new()
	{
		Name = entity.Name,
		Subjects = entity.Subjects.Select(ToDTO)
	};

	public static Models.API.Subject ToDTO(this Subject entity) => new(entity.Name);
}
