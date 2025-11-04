using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DataValidation;

public class UriValidator : ValidationAttribute
{
	private readonly Regex? _regex;

	public UriValidator(string regEx) => _regex = new Regex(regEx);
	public UriValidator(Regex? regEx) => _regex = regEx;
	public UriValidator() => _regex = null;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is null)
		{
			return new ValidationResult("Member cannot be null", [ validationContext.MemberName! ]);
		}
		else if (_regex is not null)
		{
			if (value is Uri uri)
			{
				return Match(uri.AbsoluteUri);
			}
			else if (value is string uriString)
			{
				return Match(uriString);
			}
			else
			{
				return new ValidationResult("Member cannot be tested");
			}
		}
		else
		{
			return new ValidationResult("Default case");
		}
	}

	private ValidationResult? Match(string str)
	{
		if (_regex is null) return null;
		return _regex.IsMatch(str)
					? ValidationResult.Success
					: new ValidationResult($"Field does not match specified RegExp {_regex.ToString()}");
	}
}
