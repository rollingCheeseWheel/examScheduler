namespace examScheduler;

public static class Extensions
{
	public static string RegisterFormat(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-dd");
	}

}
