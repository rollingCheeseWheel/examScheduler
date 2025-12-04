using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Entities;

public class RefreshTokenSession : IComparable<RefreshTokenSession>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();
	[Required]
	public required DateTimeOffset ExpirationDate { get; set; }
	[Required]
	public required string TokenValue { get; set; }
	[Required]
	public required Guid UserProfileId { get; set; }


	public static bool operator ==(RefreshTokenSession? a, RefreshTokenSession? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.ExpirationDate == b.ExpirationDate
			&& a.TokenValue == b.TokenValue
			&& a.UserProfileId == b.UserProfileId;
	}
	public static bool operator !=(RefreshTokenSession? a, RefreshTokenSession? b) => !( a == b );
	public override bool Equals(object? obj) => obj is RefreshTokenSession other && this == other;
	public override int GetHashCode() => HashCode.Combine(ExpirationDate, TokenValue, UserProfileId);
	public int CompareTo(RefreshTokenSession? other) => Id.CompareTo(other?.Id);
}
