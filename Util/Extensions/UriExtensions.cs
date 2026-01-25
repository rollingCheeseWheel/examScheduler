namespace Util.Extensions;

public static class UriExtensions
{
	public static Uri GetSchemeAndAuthority(this Uri uri) => new(uri.Scheme + Uri.SchemeDelimiter + uri.Authority);

	public static Uri AppendRelativePath(this Uri uri, string relativePath)
	{
		var output = new Uri(uri.ToString() + ( relativePath.StartsWith('/') || uri.ToString().EndsWith('/') ? "" : "/" ) + relativePath);
		return output;
	}
}
