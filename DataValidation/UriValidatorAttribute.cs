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
        if (value is null)
        {
            return new ValidationResult("Member cannot be null", [ validationContext.MemberName! ]);
        }
        else
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
    }

    private ValidationResult? Match(string str)
    {
        if (_regex is null) return null;
        return _regex.IsMatch(str)
                    ? ValidationResult.Success
                    : new ValidationResult($"Field does not match specified RegExp {_regex}");
    }
}
