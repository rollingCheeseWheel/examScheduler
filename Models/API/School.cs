using DataValidation;
using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class School
{
	[Required]
	public required string ClientId { get; set; }
	[Required, Url, UriValidator]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required]
	public required bool IsEnabled { get; set; }
}
