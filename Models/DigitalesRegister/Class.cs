using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util.Converters;

namespace Models.DigitalesRegister;

public class RegisterClass
{
	[Required]
	public required int Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[JsonPropertyName("group")]
	public bool? IsGroup { get; set; }
}

public class RegisterSubject
{
	[Required]
	public required int Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[JsonPropertyName("belongs_to_class")]
	public int? BelongsToClass { get; set; }
}

public class RegisterLessonSubstitute
{
	[Required, JsonConverter(typeof(RegisterDateTimeOffsetConverter))]
	public required DateTimeOffset Date { get; set; }
	[Required]
	public required int Hour { get; set; }
	[Required, JsonPropertyName("class")]
	public required string ClassName { get; set; }
	[Required, JsonPropertyName("firstName")]
	public required string TeacherFirstName { get; set; }
	[Required, JsonPropertyName("lastName")]
	public required string TeacherLastName { get; set; }
}