using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace examScheduler.Identity;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class EnumClaimAttribute<T>(string claimName, T requiredFlags)
	: Attribute, IAuthorizationFilter where T : struct, Enum
{
	public readonly T RequiredFlags = requiredFlags;
	public readonly string ClaimName = claimName;

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var httpContext = context.HttpContext;
		var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();

		if (
			string.IsNullOrEmpty(authHeader) ||
			!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
		)
		{
			context.Result = new UnauthorizedResult();
			return;
		}

		var token = authHeader[ "Bearer ".Length.. ].Trim();
		var tokenHandler = new JwtSecurityTokenHandler();

		// Resolve validation parameters (must be registered in DI)
		var validationParameters = httpContext.RequestServices.GetRequiredService<TokenValidationParameters>();

		try
		{
			// Validate signature, issuer, audience, expiration, etc.
			var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

			var claim = principal.FindFirst(ClaimName)?.Value;
			if (claim is null || !Enum.TryParse<T>(claim, out var userClaimValue))
			{
				context.Result = new ForbidResult();
				return;
			}

			if (!userClaimValue.HasFlag(RequiredFlags))
			{
				context.Result = new ForbidResult();
				return;
			}

			// Attach validated principal to context (optional)
			httpContext.User = principal;
		}
		catch (SecurityTokenExpiredException)
		{
			context.Result = new UnauthorizedObjectResult("Token expired");
		}
		catch (SecurityTokenException)
		{
			context.Result = new UnauthorizedObjectResult("Invalid token");
		}
		catch
		{
			context.Result = new UnauthorizedResult();
		}
	}
}
