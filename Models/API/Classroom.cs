using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class Classroom
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int StudentCount { get; set; }
	[Required]
	public required Calendar Calendar { get; set; }
}