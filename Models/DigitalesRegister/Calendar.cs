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

public class Lesson
{
	public required int? Id { get; set; }
	[JsonPropertyName("ttcid")]
	public required int TTCID { get; set; }
	[JsonConverter(typeof(RegisterDateConverter))]
	public required DateTime Date { get; set; }
	public required int Hour { get; set; }
	public required int ToHour { get; set; }
	public required int ClassId { get; set; }
	public required string ClassName { get; set; }
	public required Models.DigitalesRegister.Teacher[ ] Teachers { get; set; }
	public required Subject Subject { get; set; }

	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }
}

public struct Subject
{
	public required int Id { get; set; }
	public required string Name { get; set; }
}