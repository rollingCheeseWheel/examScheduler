using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util;

public static class Constants
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    public const string PermissionClaimName = "permissions";
    public const string ClassroomIdClaimName = "classroomId";
    public const string StudentIdClaimName = "studentId";
}
