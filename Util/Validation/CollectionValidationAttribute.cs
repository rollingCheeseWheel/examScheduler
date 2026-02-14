using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class CollectionValidationAttribute<TTarget>(string childPropertyName) : ValidationAttribute
{
	private readonly string _childPropertyName = childPropertyName;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is null || value is not IEnumerable<TTarget> asEnumerable)
		{
			return new($"Collection is null or not a collection of type '{typeof(TTarget).Name}'", [ validationContext.MemberName ?? "" ]);
		}

		var property = typeof(TTarget).GetProperty(_childPropertyName) ?? throw new ArgumentException($"'{_childPropertyName}' not found on '{typeof(TTarget).Name}'");
		var results = new List<ValidationResult>();
		foreach (var child in asEnumerable)
		{
			if (child is null)
			{
				results.Add(new("Child cannot be null", [ _childPropertyName ]));
				continue;
			}
			var context = new ValidationContext(child) { MemberName = _childPropertyName };
			var tempRes = new List<ValidationResult>();
			if (!Validator.TryValidateProperty(property.GetValue(child), context, tempRes))
			{
				results.AddRange(tempRes);
			}
		}
		return results.Count == 0
			? ValidationResult.Success
			: new(
				string.Join("; ", results.Select(x => x.ErrorMessage)),
				results.SelectMany(x => x.MemberNames)
			);
	}
}
