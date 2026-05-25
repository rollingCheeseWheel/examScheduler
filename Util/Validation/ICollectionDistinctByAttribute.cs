using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class ICollectionDistinctByAttribute<T>(string propertyName) : AssertTypeAttribute<ICollection<T>>
{
	public override ValidationResult? IsValid(ICollection<T> value, ValidationContext validationContext)
	{
		var type = typeof(T);
		if (propertyName.StartsWith(type.Name))
		{
			propertyName = propertyName[ type.Name.Length.. ];
		}
		var property = type.GetProperty(propertyName) ?? throw new ArgumentException(nameof(propertyName));

		var values = value.ToList();

		var hasDuplicates = values.DistinctBy(x => property.GetValue(x)).Count() < values.Count;
		return hasDuplicates ? new($"Values are not distinct by {_propertyName}") : ValidationResult.Success;
	}
}

public class ICollectionDistinctAttribute<T>() : AssertTypeAttribute<ICollection<T>>
{
	public override ValidationResult? IsValid(ICollection<T> value, ValidationContext validationContext) => value.Distinct().Count() != value.Count() ? new("Values are not distinct") : ValidationResult.Success;
}
