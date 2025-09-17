using ExamScheduler.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace examScheduler.Identity;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionClaimAttribute
	: Attribute, IAuthorizationFilter
{
	public readonly Permission RequiredPermissions;

	public PermissionClaimAttribute(Permission requiredPermissions)
	{
		RequiredPermissions = requiredPermissions;
	}

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var user = context.HttpContext.User;

		if (user.Identity is null || !user.Identity.IsAuthenticated)
		{
			context.Result = new ForbidResult();
			return;
		}

		var permissionClaim = user.FindFirst(IdentityHelper.PermissionClaimName)?.Value;
		if (permissionClaim is null || !Enum.TryParse<Permission>(permissionClaim, out var userPermissions))
		{
			context.Result = new ForbidResult();
			return;
		}

		if ((userPermissions & RequiredPermissions) != RequiredPermissions)
		{
			context.Result = new ForbidResult();
			return;
		}
	}
}
