using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Entities;

[method: SetsRequiredMembers]
public class Subject(string name) : IComparable<Subject>, IEquatable<Subject>
{
	[Key]
	public required string Name { get; set; } = name;

	public override bool Equals(object? obj) => obj is Subject asSubject && Equals(asSubject);
	public bool Equals(Subject? other) => Name == other?.Name;
	public override int GetHashCode() => HashCode.Combine(Name);
	public int CompareTo(Subject? other) => Name.CompareTo(other?.Name);
}