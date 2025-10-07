using Util;

namespace registerClient.Models;

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
	public int isLesson { get; set; }
	public Lesson lesson { get; set; }
	public int hour { get; set; }
	public int linkedHoursCount { get; set; }
}

public class Lesson
{
	public int id { get; set; }
	public int ttcid { get; set; }
	public string date { get; set; }
	public int hour { get; set; }
	public int toHour { get; set; }
	public int classId { get; set; }
	public string className { get; set; }
	public Teacher[ ] teachers { get; set; }
	public Subject subject { get; set; }
	public int linkToPreviousHour { get; set; }
}

public class Subject
{
	public int id { get; set; }
	public string name { get; set; }
	public int lernfeld { get; set; }
	public string defaultLessonContent { get; set; }
	public int defaultLessonContentType { get; set; }
}

public class Teacher
{
	public int id { get; set; }
	public string firstName { get; set; }
	public string lastName { get; set; }
}
