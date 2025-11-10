using Models.DigitalesRegister;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class UserProfile
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required School School { get; init; }
	public int SchoolId { get; }
	[Required]
	public required string RegisterUsername { get; init; }
	[Required]
	public required string DisplayName { get; set; }

	[Required]
	// Argon2id stores the salt in the encoded string
	// no unique required since the salt already has 2^(8*16) different combinations
	// and collisions do not garantee the same password and hash
	public required PasswordHash Hash { get; set; }

	// Permissions - enum flags, can be combined
	// e.g. Permission.Read | Permission.Write = 3
	[Required]
	public required UserPermissions Permissions { get; set; }
	[Required]
	public required UserProfileRoles Role { get; init; }

	public static bool operator ==(UserProfile? a, UserProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.School == b.School
			&& a.RegisterUsername == b.RegisterUsername;
	}

	public static bool operator !=(UserProfile? a, UserProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is UserProfile other && this == other;
	public bool MatchesRegisterProfile(RegisterProfileModel model) => RegisterUsername == model.Username && DisplayName == model.Name;

	public override int GetHashCode() => HashCode.Combine(School, RegisterUsername);
}