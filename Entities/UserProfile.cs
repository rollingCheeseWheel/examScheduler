using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class UserProfile
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required int RegisterId { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string RegisterUsername { get; set; }
	[Required]
	public required string DisplayName { get; set; }

	[Required]
	// Argon2id stores the salt in the encoded string
	// no unique required since the salt already has 2^(8*16) different combinations 
	public required string Hash { get; set; }

	// Permissions - enum flags, can be combined
	// e.g. Permission.Read | Permission.Write = 3
	[Required]
	public required UserPermissions Permissions { get; set; }
	[Required]
	public required UserProfileRoles Role { get; set; }

	public static bool operator ==(UserProfile? a, UserProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterUri == b.RegisterUri
			&& a.RegisterUsername == b.RegisterUsername
			&& a.RegisterId == b.RegisterId;
	}

	public static bool operator !=(UserProfile? a, UserProfile? b) => !( a == b );
	public override int GetHashCode() => HashCode.Combine(RegisterUri, RegisterUsername, RegisterId);
	public override bool Equals(object? obj) => obj is UserProfile other && this == other;
}