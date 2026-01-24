using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class UserProfile
	: IdentityUser<Guid>,
	IComparable<UserProfile>, IEquatable<UserProfile> // would be EntityBase<UserProfile>
{
	[Required]
	public required School School { get; set; }
	public Guid SchoolId { get; private set; }
	[Required]
	public required long RegiserId { get; set; }
	[Required]
	public required UserRoles Role { get; set; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }

	[NotMapped]
	public string Name => string.Join(" ", FirstName, LastName);

	public StudentProfile? StudentProfile { get; set; }
	public TeacherProfile? TeacherProfile { get; set; }

	public static bool operator ==(UserProfile? a, UserProfile? b)
	{
		return ReferenceEquals(a, b) || ( a is not null && b is not null && a.School == b.School
			&& a.UserName == b.UserName );
	}
	public static bool operator !=(UserProfile? a, UserProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is UserProfile other && this == other;

	public bool Equals(UserProfile? other) => this == other;

	public override int GetHashCode() => HashCode.Combine(School, UserName);

	public int CompareTo(UserProfile? other) => Name.CompareTo(other?.Name);
}