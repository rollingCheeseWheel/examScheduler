using System.ComponentModel.DataAnnotations;

namespace Util.Validation;


public class GreaterThanAttribute<T>(string propertyName) : ValidationAttribute where T : IComparable<T>
{
	private readonly string _propertyName = propertyName;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		var property = validationContext.ObjectType.GetProperty(_propertyName);
		var compareToValue = property?.GetValue(validationContext.ObjectInstance);
		return value is T valueAsComparable && compareToValue is T castCompareToValue
			? valueAsComparable.CompareTo(castCompareToValue) > 0
				? ValidationResult.Success
				: new("The instance is less or equal to the other")
			: new("One of the values is null or doesn't implement IComparable");
	}
}
