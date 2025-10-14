using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Models.DigitalesRegister;

public class RegisterProfile
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	[StringLength(100)]
	public required string Username { get; set; }
	[StringLength(255)]
	[Required]
	public required string RoleName { get; set; }
	[Required]
	[StringLength(255)]
	public required string Name { get; set; }
	[Required]
	[StringLength(255)]
	public required string Email { get; set; }
	[Required]
	[StringLength(255)]
	public required string Picture { get; set; }
	public string? Language { get; set; }
}