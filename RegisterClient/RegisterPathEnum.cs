using Util;

namespace registerClient;
public sealed class RegisterPath(string value) : StringEnum(value)
{
	public static readonly RegisterPath Api = new("v2/api");

	public static readonly RegisterPath LoginPage = new("login");

	public static readonly RegisterPath Login = new("auth/login");
	public static readonly RegisterPath Calendar = new("calendar/student");
	public static readonly RegisterPath ProfileDetails = new("profile/get");

	public override string ToString()
	{
		return base.ToString().EndsWith('/')
			? throw new Exception("Paths cannot end in slashes")
			: base.ToString();
	}
}