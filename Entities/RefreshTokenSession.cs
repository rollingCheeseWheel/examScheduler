using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Entities;

public class RefreshTokenSession : EntityBase<RefreshTokenSession>
{
	[Required]
	public required DateTimeOffset ExpirationDate { get; set; }
	[Required]
	public required string TokenValue { get; set; }
	[Required]
	public required Guid UserProfileId { get; set; }

	public override bool EqualsCore(RefreshTokenSession b) =>
		ExpirationDate == b.ExpirationDate &&
		TokenValue == b.TokenValue &&
		UserProfileId == b.UserProfileId;
	public override int GetHashCode() => HashCode.Combine(ExpirationDate, TokenValue, UserProfileId);
	public override int CompareTo(RefreshTokenSession? b) => ExpirationDate.CompareTo(b?.ExpirationDate ?? DateTimeOffset.MinValue);
}
