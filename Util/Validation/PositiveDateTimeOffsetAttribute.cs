using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class PositiveDateTimeOffsetAttribute(int secondsTolerance = 0) : ValidationAttribute
{
	private readonly int _secondsTolerance = secondsTolerance;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		var isValid = value is DateTimeOffset asDate && asDate >= DateTimeOffset.UtcNow.AddSeconds(-_secondsTolerance);
		return isValid ? ValidationResult.Success : new("Date cannot be in the past");
	}
}
