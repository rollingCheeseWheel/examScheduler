namespace Util;

public interface IRegisterPath
{
	Uri GetAPI(Uri @base);
	Uri GetPath(Uri @base, string path) => GetAPI(@base).AppendRelativePath(path);
}

public sealed class RegisterPathWeb(string value) : StringEnum(value), IRegisterPath
{
	public static readonly RegisterPathWeb Api = new("v2/api");

	public static readonly RegisterPathWeb LoginPage = new("login");

	public static readonly RegisterPathWeb Login = new("auth/login");
	public static readonly RegisterPathWeb Calendar = new("calendar/student");
	public static readonly RegisterPathWeb ProfileDetails = new("profile/get");

	public Uri GetAPI(Uri @base) => @base.AppendRelativePath(Api);

	public override string ToString()
	{
		return base.ToString().EndsWith('/')
			? throw new Exception("Paths cannot end in slashes")
			: base.ToString();
	}
}

public sealed class RegisterPathAPI(string value) : StringEnum(value), IRegisterPath
{
	public static readonly RegisterPathAPI Api = new("v2/api/v1");

	public Uri GetAPI(Uri @base) => @base.AppendRelativePath(Api);
}