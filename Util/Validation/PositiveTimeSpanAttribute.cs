using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class PositiveTimeSpanAttribute : AssertTypeAttribute<TimeSpan>
{
	public override ValidationResult? IsValid(TimeSpan value, ValidationContext validationContext) => value >= TimeSpan.Zero ? ValidationResult.Success : new("TimeSpan is negative");
}
