using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class Classroom
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required]
	public required IEnumerable<TeacherWithSubjects> Teachers { get; set; }
}