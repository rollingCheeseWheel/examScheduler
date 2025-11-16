namespace Util;

public abstract class RegisterPath(string value) : StringEnum(value)
{
	public abstract	Uri GetAPI(Uri @base);
	public static Uri GetPath(Uri @base, RegisterPath path) => path.GetAPI(@base).AppendRelativePath(path);
	public Uri Get(Uri @base) => GetPath(@base, this);
}

public sealed class RegisterPathAPI(string value) : RegisterPath(value)
{
	public static readonly RegisterPathAPI Api = new("v2/api/v1");
	public static readonly RegisterPathAPI TokenCreate = new("token");
	public static readonly RegisterPathAPI TokenRefresh = new("refresh_token");

	public static readonly RegisterPathAPI UserProfile = new("user/me");

	public static readonly RegisterPathAPI Classes = new("class/all");
	public static readonly RegisterPathAPI Subjects = new("subject/all");

	public static readonly RegisterPathAPI LessonMonth = new("lesson/my_lesson");
	public static readonly RegisterPathAPI LessonWeek = new("lesson/my_calendar");

	public override Uri GetAPI(Uri @base) => @base.GetSchemeAndAuthority().AppendRelativePath(Api);
}