using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class DefinedEnumAttribute(bool canBeNull = false) : ValidationAttribute
{
	private readonly bool _canBeNull = canBeNull;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is null && _canBeNull)
		{
			return ValidationResult.Success;
		}
		else if (value is null && !_canBeNull)
		{
			return new("Value cannot be null");
		}

		return Enum.IsDefined(value!.GetType(), value)
			? ValidationResult.Success
			: new($"Enum value is not defined for enum {value.GetType().Name}");
	}
}
