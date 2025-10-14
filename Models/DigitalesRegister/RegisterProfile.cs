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
	public required string Username { get; set; }
	[Required]
	public required string RoleName { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required]
	public required string Email { get; set; }
	[Required]
	public required string Picture { get; set; }
	public string? Language { get; set; }
}