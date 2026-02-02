using System.ComponentModel.DataAnnotations;

namespace Util.Validation;

public class DefinedGuidAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => value is Guid asGuid && asGuid != Guid.Empty
			? ValidationResult.Success
			: value is string asString && Guid.TryParse(asString, out var parsedGuid) && parsedGuid != Guid.Empty
			? ValidationResult.Success
			: new("Invalid GUID");
}
