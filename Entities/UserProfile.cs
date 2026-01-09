using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class UserProfile : IdentityUser<Guid>, IComparable<UserProfile>
{
	[Required]
	public required School School { get; init; }
	public Guid SchoolId { get; }
	[Required]
	public required long RegiserId { get; init; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[Required]
	public required UserRole Role { get; init; }

	[NotMapped]
	public string Name => $"{FirstName} {LastName}";

	public StudentProfile? StudentProfile { get; init; }
	public TeacherProfile? TeacherProfile { get; init; }

	public static bool operator ==(UserProfile? a, UserProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.School == b.School
			&& a.UserName == b.UserName;
	}
	public static bool operator !=(UserProfile? a, UserProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is UserProfile other && this == other;
	public override int GetHashCode() => HashCode.Combine(School, UserName);
	public int CompareTo(UserProfile? other)
	{
		if (other is null) { return 1; }
		var res = FirstName.CompareTo(other.FirstName);
		if (res != 0) { return res; }
		res = LastName.CompareTo(other.LastName);
		if (res != 0) { return res; }
		return School.CompareTo(other.School);
	}
}