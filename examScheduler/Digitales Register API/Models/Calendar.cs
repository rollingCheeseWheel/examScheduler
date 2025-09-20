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
	
}