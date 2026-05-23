using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class SameDayOfWeekAttribute : AssertTypeAttribute<IEnumerable<DateOnly>>
{
	public override ValidationResult? IsValid(IEnumerable<DateOnly> value, ValidationContext validationContext)
	{
		var dates = value.ToList();

		if (dates.Count <= 1)
		{
			return ValidationResult.Success;
		}

		var first = dates[ 0 ].DayOfWeek;
		return value.All(d => d.DayOfWeek == first) ? ValidationResult.Success : new("Not all entries have the same day of week");
	}
}
