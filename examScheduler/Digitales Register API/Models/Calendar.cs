namespace examScheduler.Digitales_Register_API.Models;

public class CalendarRequest
{
	/// <summary>
	/// The date formatted as YYYY-MM-DD
	/// </summary>
	public required string StartDate { get; set; }

	public CalendarRequest(DateTime startDate)
	{
		StartDate = startDate.ToString("yyyy-MM-dd");
	}
}
