using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public abstract class AssertTypeAttribute<T> : ValidationAttribute
{
	protected readonly string? _propertyName;

	protected AssertTypeAttribute() { }
	protected AssertTypeAttribute(string propertyName) => _propertyName = propertyName;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (_propertyName is not null)
		{
			var property = validationContext.ObjectType.GetProperty(_propertyName) ?? throw new ArgumentException(nameof(_propertyName));
			value = property.GetValue(value);
		}

		if (value is not T cast)
		{
			return new($"Value is null or not of type {typeof(T).Name}");
		}
		return IsValid(cast, validationContext);
	}

	public abstract ValidationResult? IsValid(T value, ValidationContext validationContext);
}
