using System.ComponentModel.DataAnnotations;

namespace Models.API.School;

public class School
{
	[Required]
	public required string Name { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
}
