using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class MinValueAttribute(long min) : ValidationAttribute
{
	private readonly long _min = min;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is null)
		{
			return new("Cannot be null");
		}

		var valueType = value.GetType();

		var comparer = (IComparer?)typeof(Comparer<>)
			.MakeGenericType(valueType)
			.GetProperty("Default")
			?.GetValue(null);
		if (comparer is null)
		{
			return new("Value is not comparable");
		}

		object min;
		try
		{
			min = Convert.ChangeType(_min, valueType);
		}
		catch
		{
			return new("Mininum value type mismatch");
		}

		var isValid = comparer.Compare(value, min) >= 0;
		return isValid ? ValidationResult.Success : new($"Value must be >= {_min}");
	}
}
