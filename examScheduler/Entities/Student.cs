using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class Student
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	// Unique
	public required string RegisterUsername { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required]
	public required string Surname { get; set; }
	public DateTime CreatedAt { get; set; }

	[Required]
	public required string Salt { get; set; }
	[Required]
	public required string Hash { get; set; }

	// Permissions - enum flags, can be combined
	// e.g. Permission.Read | Permission.Write = 3
	[Required]
	public required Permission Permissions { get; set; }

	// Navigation Properties
	[Required]
	public required Classroom Classroom { get; set; }
}
