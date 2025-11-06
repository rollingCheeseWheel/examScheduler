using Models.DigitalesRegister;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Entities;

public class UserProfile : IdentityUser<int>
{
	[Required]
	public required School School { get; init; }
	public int SchoolId { get; }
	[Required]
	public required string DisplayName { get; set; }

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
	public bool MatchesRegisterProfile(RegisterProfileModel model) => UserName == model.Username && DisplayName == model.Name;
	public override int GetHashCode() => HashCode.Combine(School, UserName);
}