namespace examScheduler.Digitales_Register_API.Models;

public class CalendarRequest
{
	/// <summary>
	/// The date formatted as YYYY-MM-DD
	/// </summary>
	public string StartDate { get; private set; }

	public CalendarRequest(DateTime startDate)
	{
		StartDate = startDate.ToRegisterFormat();
	}
}

public class CalendarDay
{
	public DateTime Date { get; set; }
	public List<HourInDay> HoursInDay { get; set; } = new();
}


public class HourInDay
{
	public required int IsLesson { get; set; }
	public Lesson? Lesson { get; set; }
	public required int Hour { get; set; }
	public required int LinkedHoursCount { get; set; }
}

public class Lesson
{
	public required int Id { get; set; }
	public required int Ttcid { get; set; }
	public required string Date { get; set; }
	public required int Hour { get; set; }
	public required int ToHour { get; set; }
	public required int TimeStart { get; set; }
	public required int TimeEnd { get; set; }
	public required int TimeToEnd { get; set; }
	public required TimeObject TimeStartObject { get; set; }
	public required TimeObject TimeEndObject { get; set; }
	public required TimeObject TimeToEndObject { get; set; }
	public required bool TimeShowEnabled { get; set; }
	public required int ClassId { get; set; }
	public required string ClassName { get; set; }
	public required string ClassComment { get; set; }
	public required string Description { get; set; }
	public required string Note { get; set; }
	public required bool LessonShow { get; set; }
	public required Teacher[ ] Teachers { get; set; }
	public required object[ ] TeachersToNotify { get; set; }
	public required object TeacherMyself { get; set; }
	public required bool IsAutoNotify { get; set; }
	public required bool IsLessonTypeNotifyOn { get; set; }
	public required bool Exp_lt_default { get; set; }
	public required bool IsSecretary { get; set; }
	public required Subject Subject { get; set; }
	public required object[ ] HomeworkExams { get; set; }
	public required Lessoncontent[ ] LessonContents { get; set; }
	public required object[ ] Rooms { get; set; }
	public required bool ReadOnly { get; set; }
	public required int IsSubstitute { get; set; }
	public required int LinkToPreviousHour { get; set; }
	public required object[ ] LinkedHours { get; set; }
	public required object[ ] CriticalObservations { get; set; }
	public required object[ ] MissingStudents { get; set; }
	public required object[ ] Students { get; set; }
	public required object[ ] Grades { get; set; }
	public required object[ ] Observations { get; set; }
	public required object[ ] AbsenceOpenAbsencesStudents { get; set; }
}

public class TimeObject
{
	public required string H { get; set; }
	public required string M { get; set; }
	public required int Ts { get; set; }
	public required string Text { get; set; }
	public required string Html { get; set; }
}

public class Subject
{
	public required int Id { get; set; }
	public required string Name { get; set; }
	public required int Lernfeld { get; set; }
	public required string DefaultLessonContent { get; set; }
	public required int DefaultLessonContentType { get; set; }
}

public class Teacher
{
	public required int Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
}

public class Lessoncontent
{
	public required int Id { get; set; }
	public required string Name { get; set; }
	public required int Homework { get; set; }
	public required int Online { get; set; }
	public required object DeadlineStart { get; set; }
	public required object Deadline { get; set; }
	public required object DeadlineOvertime { get; set; }
	public required bool HasLessonContentSubmissions { get; set; }
	public required int TypeId { get; set; }
	public required string TypeName { get; set; }
	public required object[ ] LessonContentSubmissions { get; set; }
	public required object[ ] LessonContentStudents { get; set; }
	public required int LessonContentStudentsPercentage { get; set; }
}
