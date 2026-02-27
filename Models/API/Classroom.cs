using System.ComponentModel.DataAnnotations;
using Util.Validation;

namespace Models.API;

public class Classroom
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required, MinValue(0)]
	public required int StudentCount { get; set; }
	//[Required]
	//public required Calendar Calendar { get; set; }
}