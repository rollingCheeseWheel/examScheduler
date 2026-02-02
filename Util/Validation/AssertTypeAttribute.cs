using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public abstract class AssertTypeAttribute<T> : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is not T cast)
		{
			return new($"Value is null or not of type {typeof(T).Name}");
		}
		return IsValid(cast, validationContext);
	}

	public abstract ValidationResult? IsValid(T value, ValidationContext validationContext);
}
