using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using Util;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public class UserProfile
	: IdentityUser<Guid>,
	IComparable<UserProfile>, IEquatable<UserProfile>, // would be EntityBase<UserProfile>
	IGuidEntity
{
	[Required]
	public required string SchoolId { get; set; }
	[Required]
	public required long RegiserId { get; set; }
	[Required, DefinedEnum]
	public required UserRoles Role { get; set; }

	[Required]
	public required string Name { get; set; }

	[Required]
	public ICollection<Guid> ClassroomIds { get; set; } = [ ];

	public StudentProfile? StudentProfile { get; set; }
	public TeacherProfile? TeacherProfile { get; set; }

	public static bool operator ==(UserProfile? a, UserProfile? b)
	{
		return ReferenceEquals(a, b) || ( a is not null && b is not null && a.SchoolId == b.SchoolId
			&& a.UserName == b.UserName );
	}
	public static bool operator !=(UserProfile? a, UserProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is UserProfile other && this == other;

	public bool Equals(UserProfile? other) => this == other;

	public override int GetHashCode() => HashCode.Combine(SchoolId, UserName);

	public int CompareTo(UserProfile? other) => Name.CompareTo(other?.Name);
}