using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class PositiveTimeSpanAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => value is TimeSpan asTimeSpan
			? asTimeSpan >= TimeSpan.Zero
				? ValidationResult.Success
				: new("TimeSpan is negative")
			: new("Value is null or not a TimeSpan");
}
