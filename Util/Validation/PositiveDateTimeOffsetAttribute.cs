using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class PositiveDateTimeOffsetAttribute(int secondsTolerance = 0) : AssertTypeAttribute<DateTimeOffset>
{
	private readonly int _secondsTolerance = secondsTolerance;

	public override ValidationResult? IsValid(DateTimeOffset value, ValidationContext validationContext) => value >= DateTimeOffset.UtcNow.AddSeconds(-_secondsTolerance) ? ValidationResult.Success : new("Date cannot be in the past");
}
