using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class DistinctByAttribute<T>(string propertyName) : AssertTypeAttribute<IEnumerable<T>>
{
	public override ValidationResult? IsValid(IEnumerable<T> value, ValidationContext validationContext)
	{
		var property = typeof(T).GetProperty(propertyName) ?? throw new ArgumentException(nameof(propertyName));

		var hasDuplicates = value.DistinctBy(x => property.GetValue(x)).Count() < value.Count();
		return hasDuplicates ? new($"Values are not distinct by {_propertyName}") : ValidationResult.Success;
	}
}

public class DistinctAttribute<T>() : AssertTypeAttribute<IEnumerable<T>>
{
	public override ValidationResult? IsValid(IEnumerable<T> value, ValidationContext validationContext) => value.Distinct().Count() != value.Count() ? new("Values are not distinct") : ValidationResult.Success;
}
