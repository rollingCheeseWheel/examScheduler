using System.Text.Json;

namespace examScheduler;

public static class Constants
{
	public static JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
	};
}
