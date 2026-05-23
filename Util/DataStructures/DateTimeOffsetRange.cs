using Util.Extensions;

namespace Util.DataStructures;

public record struct DateTimeOffsetRange(DateTimeOffset? Start, DateTimeOffset? End)
{
	public readonly bool Contains(DateTimeOffset value) => ( Start ?? DateTimeOffset.MinValue ) <= value && value <= ( End ?? DateTimeOffset.MaxValue );

	public readonly bool Contains(DateOnly value) => ( Start ?? DateTime.MinValue ).ToDateOnly() <= value && value <= DateOnly.FromDateTime(End?.DateTime ?? DateTime.MaxValue);

	public readonly bool Contains(DateTime value) => ( Start ?? DateTimeOffset.MinValue ) <= value.ToUniversalTime() && value.ToUniversalTime() <= ( End ?? DateTimeOffset.MaxValue );
}
