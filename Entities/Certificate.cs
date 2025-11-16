using System.ComponentModel.DataAnnotations;

namespace Entities;

public class Certificate
{
	[Key]
	public required int Key { get; set; }
	public required byte[ ] Bytes { get; set; }
}
