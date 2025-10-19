using Models.DigitalesRegister;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Student
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string RegisterUsername { get; set; }
	[NotMapped]
	public string DisplayName { get => RegisterProfile.Name; }
	[Required]
	public DateTime CreatedAt { get; set; } = DateTime.Now;

	[Required]
	[StringLength(255)]
	// Argon2id stores the salt in the encoded string
	public required string Hash { get; set; }

	// Permissions - enum flags, can be combined
	// e.g. Permission.Read | Permission.Write = 3
	[Required]
	public required Permission Permissions { get; set; }

	// Navigation Properties
	[Required]
	public required Classroom Classroom { get; set; }
	[Required]
	public required RegisterProfile RegisterProfile { get; set; }
	/// <summary>
	/// to convince EF of a many-to-many relationship
	/// a Student has a Classroom, a Classroom has many (indirect) ExamSlots, an ExamSlot has many Students
	/// thus a Student has many ExamSlots
	/// </summary>
	[Required]
	public ICollection<ExamSlot> ExamSlots { get; set; } = [ ];
	
}
