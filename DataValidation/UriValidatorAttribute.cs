using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DataValidation;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
public class UriValidatorAttribute(Regex regEx) : ValidationAttribute
{
	private readonly Regex _regex = regEx;

	public UriValidatorAttribute() : this(@"^https://.+?digitalesregister.it/?$", RegexOptions.IgnoreCase, 50) { }
	public UriValidatorAttribute(string regEx) : this(new Regex(regEx)) { }
	public UriValidatorAttribute(string pattern, RegexOptions options, int timeoutMillis) : this(new Regex(pattern, options, TimeSpan.FromMilliseconds(timeoutMillis))) { }
	public UriValidatorAttribute(string pattern, RegexOptions options, TimeSpan timeSpan) : this(new Regex(pattern, options, timeSpan)) { }

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		return value is null
			? new ValidationResult("Member cannot be null", [ validationContext.MemberName! ])
			: value is Uri uri
				? Match(uri.AbsoluteUri)
				: value is string uriString ? Match(uriString) : new ValidationResult("Member cannot be tested");
	}

	private ValidationResult? Match(string str)
	{
		return _regex is null
			? null
			: _regex.IsMatch(str)
					? ValidationResult.Success
					: new ValidationResult($"Field does not match specified RegExp {_regex}");
	}
}
