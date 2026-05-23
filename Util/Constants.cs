using System.Text.Json;

namespace Util;

public static class Constants
{
	public static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true
	};
}
