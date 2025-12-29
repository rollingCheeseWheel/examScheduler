using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class TeacherProfile : IComparable<TeacherProfile>
{
	[Key] // is the same key as the userprofile
	public Guid Id { get; private set; }

	// Navigation Properties
	public required UserProfile UserProfile { get; set; }

	public Teacher? Teacher { get; set; }
	public Guid? TeacherId { get; private set; }

	public static bool operator ==(TeacherProfile? a, TeacherProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.UserProfile == b.UserProfile;
	}
	public static bool operator !=(TeacherProfile? a, TeacherProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is TeacherProfile other && this == other;
	public override int GetHashCode() => UserProfile.GetHashCode();
	public int CompareTo(TeacherProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}
