namespace Util;

public class StringEnum : IEquatable<StringEnum>, IComparable<StringEnum>
{
	public readonly string Value;

	protected StringEnum(string value) => Value = value;

	public override string ToString()
	{
		return Value;
	}

	public static implicit operator string(StringEnum @enum) => @enum.ToString();
	public static bool operator ==(StringEnum? a, StringEnum? b) => ReferenceEquals(a, b) || ( a is not null && b is not null && a.Value == b.Value );
	public static bool operator !=(StringEnum? left, StringEnum? right) => !( left == right );

	public override bool Equals(object? obj)
	{
		return obj is StringEnum other && Equals(other);
	}

	public bool Equals(StringEnum? other)
	{
		return this == other;
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public int CompareTo(StringEnum? other)
	{
		return other is null ? 1 : Value.CompareTo(other.Value);
	}
}