namespace Util.Extensions;

public static class IEnumerableExtensions
{
	public static int GetValueHashCode<T>(this IEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);

		long sum = 0;
		long sumSquares = 0;
		var count = 0;

		foreach (var item in source)
		{
			var h = item?.GetHashCode() ?? 0;
			sum += h;
			sumSquares += (long)h * h;
			count++;
		}

		return HashCode.Combine(sum, sumSquares, count);
	}
}
