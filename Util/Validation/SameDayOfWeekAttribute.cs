using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class SameDayOfWeekAttribute : AssertTypeAttribute<IEnumerable<DateTimeOffset>>
{
	public override ValidationResult? IsValid(IEnumerable<DateTimeOffset> value, ValidationContext validationContext)
	{
		var first = value.FirstOrDefault();
		return value.All(d => d.DayOfWeek == first.DayOfWeek) ? ValidationResult.Success : new("Not all entries have the same day of week");
	}
}
