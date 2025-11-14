namespace Util;

public class StringEnum
{
	public readonly string Value;

	protected StringEnum(string value) => Value = value;

	public override string ToString() => Value;

	public static implicit operator string(StringEnum @enum) => @enum.ToString();
	public static bool operator ==(StringEnum? a, StringEnum? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Value == b.Value;
	}
	public static bool operator !=(StringEnum? left, StringEnum? right) => !( left == right );

	public override bool Equals(object? obj) => obj is StringEnum other && this == other;

	public override int GetHashCode() => Value.GetHashCode();
}