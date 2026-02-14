using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class GreaterThanAttribute<T>(string propertyName, bool allowNull = false) : ComparisonAttribute<T>(propertyName, allowNull)
{
	public override bool Compare(Comparer<T> comparer, T? a, T? b) => comparer.Compare(a, b) > 0;
}

public class GreaterThanOrEqualAttribute<T>(string propertyName, bool allowNull = false) : ComparisonAttribute<T>(propertyName, allowNull)
{
	public override bool Compare(Comparer<T> comparer, T? a, T? b) => comparer.Compare(a, b) >= 0;
}

public class SmallerThanAttribute<T>(string propertyName, bool allowNull = false) : ComparisonAttribute<T>(propertyName, allowNull)
{
	public override bool Compare(Comparer<T> comparer, T? a, T? b) => comparer.Compare(a, b) < 0;
}

public class SmallerThanOrEqualAttribute<T>(string propertyName, bool allowNull = false) : ComparisonAttribute<T>(propertyName, allowNull)
{
	public override bool Compare(Comparer<T> comparer, T? a, T? b) => comparer.Compare(a, b) <= 0;
}

public abstract class ComparisonAttribute<T>(string propertyName, bool allowNull = false) : ValidationAttribute
{
	private readonly string _propertyName = propertyName;
	private readonly bool _allowNull = allowNull;

	public abstract bool Compare(Comparer<T> comparer, T? a, T? b);

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		var property = validationContext.ObjectType.GetProperty(_propertyName) ?? throw new ArgumentException($"'{_propertyName}' not found on '{validationContext.ObjectType.Name}'");
		var propertyValue = property?.GetValue(validationContext.ObjectInstance);
		return !_allowNull && ( propertyValue is null || value is null )
			? new("Values cannot be null")
			: ( value is null || value is T ) && ( propertyValue is null || propertyValue is T )
			? Compare(Comparer<T>.Default, (T?)value, (T?)propertyValue) ? ValidationResult.Success : new("Equality does not match")
			: new("Values don't have matching types");
	}
}