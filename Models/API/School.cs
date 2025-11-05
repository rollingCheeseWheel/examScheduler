using DataValidation;
using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class School
{
	[Required]
	public required string Name { get; set; }
	[Required, Url, UriValidator, MaxLength(300)]
	public required Uri RegisterUri { get; set; }
}
