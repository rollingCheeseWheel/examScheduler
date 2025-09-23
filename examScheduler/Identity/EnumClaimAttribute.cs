using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace examScheduler.Identity;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class EnumClaimAttribute<T>
	: Attribute, IAuthorizationFilter where T : struct, Enum
{
	public readonly T RequiredFlags;
	public readonly string ClaimName;

	public EnumClaimAttribute(string claimName, T requiredFlags)
	{
		ClaimName = claimName;
		RequiredFlags = requiredFlags;
	}

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var user = context.HttpContext.User;

		if (user.Identity is null
			|| !user.Identity.IsAuthenticated)
		{
			context.Result = new ForbidResult();
			return;
		}

		var claim = user.FindFirst(ClaimName)?.Value;
		if (claim is null
			|| !Enum.TryParse<T>(claim, out var userClaimValue))
		{
			context.Result = new ForbidResult();
			return;
		}

		if (userClaimValue.HasFlag(RequiredFlags))
		{
			context.Result = new ForbidResult();
			return;
		}
	}
}
