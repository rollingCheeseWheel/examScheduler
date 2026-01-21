using System.ComponentModel.DataAnnotations;

namespace Entities;

public class RefreshTokenSession : EntityBase<RefreshTokenSession>
{
    [Key]
    public override Guid Id { get; set; } = Guid.NewGuid();
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
