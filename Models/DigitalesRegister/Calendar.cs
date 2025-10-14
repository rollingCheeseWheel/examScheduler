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

public class Lesson
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[JsonPropertyName("id")]
	public required int? RegisterId { get; set; }
	[JsonPropertyName("ttcid")]
	public required int TTCID { get; set; }
	[Required]
	[JsonConverter(typeof(RegisterDateConverter))]
	public required DateTime Date { get; set; }
	[Required]
	public required int Hour { get; set; }
	[Required]
	public required int ToHour { get; set; }
	[Required]
	public required int ClassId { get; set; }
	[Required]
	public required string ClassName { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required Subject Subject { get; set; }
	[Required]

	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }
}

public class Teacher
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	[JsonPropertyName("id")]
	public required int RegisterId { get; set; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }

	public override bool Equals(object? obj)
	{
		if (obj is Teacher asTeacher)
		{
			return FirstName == asTeacher.FirstName
				&& LastName == asTeacher.LastName
				&& RegisterId == asTeacher.RegisterId;
		}
		return false;
	}

	public override int GetHashCode() => base.GetHashCode();
}

public class Subject
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[JsonIgnore]
	public int Id { get; set; }
	[Required]
	public required int RegisterId { get; set; }
	[Required]
	public required string Name { get; set; }

	public override bool Equals(object? obj)
	{
		if (obj is Subject asSubject)
		{
			return RegisterId == asSubject.RegisterId && Name == asSubject.Name;
		}
		return false;
	}

	public override int GetHashCode() => base.GetHashCode();
}