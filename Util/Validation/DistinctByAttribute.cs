using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class DistinctByAttribute<T>(string propertyName) : AssertTypeAttribute<IEnumerable<T>>
{
	public override ValidationResult? IsValid(IEnumerable<T> value, ValidationContext validationContext)
	{
		var property = typeof(T).GetProperty(propertyName) ?? throw new ArgumentException(nameof(propertyName));

		var hasDuplicates = value.DistinctBy(x => property.GetValue(x)).Count() < value.Count();
		return hasDuplicates ? new($"Duplicates found in {_propertyName}") : ValidationResult.Success;
	}
}
