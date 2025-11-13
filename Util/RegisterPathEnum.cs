namespace Util;

public abstract class RegisterPath(string value) : StringEnum(value)
{
	public abstract	Uri GetAPI(Uri @base);
	public static Uri GetPath(Uri @base, RegisterPath path) => path.GetAPI(@base).AppendRelativePath(path);
	public Uri Get(Uri @base) => GetPath(@base, this);
}

[Obsolete]
public sealed class RegisterPathWeb(string value) : RegisterPath(value)
{
	public static readonly RegisterPathWeb Api = new("v2/api");

	public static readonly RegisterPathWeb LoginPage = new("login");

	public static readonly RegisterPathWeb Login = new("auth/login");
	public static readonly RegisterPathWeb Calendar = new("calendar/student");
	public static readonly RegisterPathWeb ProfileDetails = new("profile/get");

	public override Uri GetAPI(Uri @base) => @base.GetSchemeAndAuthority().AppendRelativePath(Api);

	public override string ToString()
	{
		return base.ToString().EndsWith('/')
			? throw new Exception("Paths cannot end in slashes")
			: base.ToString();
	}
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
	/// <summary>
	/// <b>IMPORTANT</b> the date needs to be formatted YYYY-MM-DD and passed as the url parameter <b>startDate</b>
	/// </summary>
	public static readonly RegisterPathAPI LessonDate = new("lesson/my_calendar");

	public override Uri GetAPI(Uri @base) => @base.GetSchemeAndAuthority().AppendRelativePath(Api);
}