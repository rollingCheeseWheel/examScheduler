using Models.DigitalesRegister;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Entities;

public class Teacher
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	[JsonPropertyName("id")]
	public required int RegisterId { get; set; }
	[Required]
	[StringLength(100)]
	public required string FirstName { get; set; }
	[Required]
	[StringLength(100)]
	public required string LastName { get; set; }

	// Navigation Properties
	public Calendar? Timetable { get; set; }
	[Required]
	public ICollection<Classroom> Classrooms { get; set; } = [ ];
	[Required]
	public ICollection<Subject> Subjects { get; set; } = [ ];

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
