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