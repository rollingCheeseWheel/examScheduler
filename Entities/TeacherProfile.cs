using System.ComponentModel.DataAnnotations;

namespace Entities;

public class TeacherProfile
{
	[Key]
	public int Id { get; private set; }

	// Navigation Properties
	[Required]
	public required UserProfile UserProfile { get; init; }

	public required Teacher? Teacher { get; init; }
	[Required]
	public int TeacherId { get; private set; }

	public static bool operator ==(TeacherProfile? a, TeacherProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.UserProfile == b.UserProfile;
	}
	public static bool operator !=(TeacherProfile? a, TeacherProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is TeacherProfile other && this == other;
	public override int GetHashCode() => UserProfile.GetHashCode();
}
