using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class ValidEnumAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => value is null
			? new("Enum value cannot be null")
			: Enum.IsDefined(value.GetType(), value)
			? ValidationResult.Success
			: new($"Enum value is not defined for enum {value.GetType().Name}");
}
