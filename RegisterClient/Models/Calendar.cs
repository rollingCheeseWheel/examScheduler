using System.Text.Json.Serialization;
using Util;

namespace registerClient.Models;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateConverter))]
	public DateTime StartDate { get; set; }
}