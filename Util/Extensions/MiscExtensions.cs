using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Util.Extensions;

public static class MiscExtensions
{
	public static bool TryValidate(this object value, out ICollection<ValidationResult> results)
	{
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(value, new(value), validationResults, true);
		results = validationResults;
		return isValid;
	}

	public static bool TryValidate(this object value) => value.TryValidate(out var _);
	public static bool TrySet<T>(this T?[ , ] grid, int firstDimension, int secondDimension, T? element)
	{
		if (firstDimension >= 0 && firstDimension < grid.GetLength(0) &&
			secondDimension >= 0 && secondDimension < grid.GetLength(1))
		{
			grid[ firstDimension, secondDimension ] = element;
			return true;
		}
		return false;
	}

	public static T? GetOrDefault<T>(this T?[ , ] grid, int firstDimension, int secondDimension) => firstDimension >= 0 && firstDimension < grid.GetLength(0) &&
			secondDimension >= 0 && secondDimension < grid.GetLength(1)
			? grid[ firstDimension, secondDimension ]
			: default;

	public static bool TryGetId(this ClaimsPrincipal claims, out Guid id)
	{
		var claim = claims.FindFirst(ClaimTypes.NameIdentifier);
		return Guid.TryParse(claim?.Value, out id);
	}
}
