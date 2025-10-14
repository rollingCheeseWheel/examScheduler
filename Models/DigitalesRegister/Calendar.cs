using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Util;

namespace Models.DigitalesRegister;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateConverter))]
	public DateTime StartDate { get; set; }
}