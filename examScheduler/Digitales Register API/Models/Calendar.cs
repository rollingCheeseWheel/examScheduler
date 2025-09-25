namespace examScheduler.Digitales_Register_API.Models;

public class CalendarRequest
{
	/// <summary>
	/// The date formatted as YYYY-MM-DD
	/// </summary>
	public string StartDate { get; private set; }

	public CalendarRequest(DateTime startDate)
	{
		StartDate = startDate.RegisterFormat();
	}
}

public class CalendarDay
{
	public DateTime Date { get; set; }
	public List<HourInDay> HoursInDay { get; set; } = new();
}


public class HourInDay
{
	public int IsLesson { get; set; }
	public Lesson? Lesson { get; set; }
	public int Hour { get; set; }
	public int LinkedHoursCount { get; set; }
}

public class Lesson
{
	public int Id { get; set; }
	public int Ttcid { get; set; }
	public string Date { get; set; }
	public int Hour { get; set; }
	public int ToHour { get; set; }
	public int TimeStart { get; set; }
	public int TimeEnd { get; set; }
	public int TimeToEnd { get; set; }
	public TimeObject TimeStartObject { get; set; }
	public TimeObject TimeEndObject { get; set; }
	public TimeObject TimeToEndObject { get; set; }
	public bool TimeShowEnabled { get; set; }
	public int ClassId { get; set; }
	public string ClassName { get; set; }
	public string ClassComment { get; set; }
	public string Description { get; set; }
	public string Note { get; set; }
	public bool LessonShow { get; set; }
	public Teacher[ ] Teachers { get; set; }
	public object[ ] TeachersToNotify { get; set; }
	public object TeacherMyself { get; set; }
	public bool IsAutoNotify { get; set; }
	public bool IsLessonTypeNotifyOn { get; set; }
	public bool Exp_lt_default { get; set; }
	public bool IsSecretary { get; set; }
	public Subject Subject { get; set; }
	public object[ ] HomeworkExams { get; set; }
	public Lessoncontent[ ] LessonContents { get; set; }
	public object[ ] Rooms { get; set; }
	public bool ReadOnly { get; set; }
	public int IsSubstitute { get; set; }
	public int LinkToPreviousHour { get; set; }
	public object[ ] LinkedHours { get; set; }
	public object[ ] CriticalObservations { get; set; }
	public object[ ] MissingStudents { get; set; }
	public object[ ] Students { get; set; }
	public object[ ] Grades { get; set; }
	public object[ ] Observations { get; set; }
	public object[ ] AbsenceOpenAbsencesStudents { get; set; }
}

public class TimeObject
{
	public string H { get; set; }
	public string M { get; set; }
	public int Ts { get; set; }
	public string Text { get; set; }
	public string Html { get; set; }
}

public class Subject
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int Lernfeld { get; set; }
	public string DefaultLessonContent { get; set; }
	public int DefaultLessonContentType { get; set; }
}

public class Teacher
{
	public int Id { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
}

public class Lessoncontent
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int Homework { get; set; }
	public int Online { get; set; }
	public object DeadlineStart { get; set; }
	public object Deadline { get; set; }
	public object DeadlineOvertime { get; set; }
	public bool HasLessonContentSubmissions { get; set; }
	public int TypeId { get; set; }
	public string TypeName { get; set; }
	public object[ ] LessonContentSubmissions { get; set; }
	public object[ ] LessonContentStudents { get; set; }
	public int LessonContentStudentsPercentage { get; set; }
}
