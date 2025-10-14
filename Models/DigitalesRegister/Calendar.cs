using System.Text.Json.Serialization;
using Util;

namespace Models.DigitalesRegister;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateConverter))]
	public DateTime StartDate { get; set; }
}

public struct Teacher
{
	public required int Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
}