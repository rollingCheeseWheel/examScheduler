using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Tracing;

namespace Util.Extensions;

public interface IGuidEntity
{
	Guid Id { get; }
}

public static class LinqExtensions
{
	public static bool ValueEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)
		where T : IComparable<T> => ValueEquals(first, second, x => x);

	public static bool ValueEquals<T, TKey>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> selector)
		where TKey : IComparable<TKey> => first.OrderBy(selector).SequenceEqual(second.OrderBy(selector));

	public static IEnumerable<TSource> Except<TSource, TKey>(this IEnumerable<TSource> source, IEnumerable<TSource> target, Func<TSource, TKey> selector) where TKey : IEquatable<TKey>
	{
		var keysInTarget = new HashSet<TKey>(target.Select(selector));
		foreach (var item in source)
		{
			if (!keysInTarget.Contains(selector(item)))
			{
				yield return item;
			}
		}
	}

	public static ICollection<T> AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			source.Add(item);
		}
		return source;
	}

	public static IEnumerable<TResult> DistinctMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector) where TResult : IEquatable<TResult> => source.SelectMany(selector).Distinct();

	public static TSource? FindById<TSource>(this IEnumerable<TSource> source, Guid id) where TSource : IGuidEntity => source.FirstOrDefault(x => x.Id == id);

	public static TSource? FindById<TSource>(this IQueryable<TSource> source, Guid id) where TSource : IGuidEntity => source.FirstOrDefault(x => x.Id == id);

	public static async Task<TSource?> FindByIdAsync<TSource>(this IQueryable<TSource> source, Guid id, CancellationToken ct = default) where TSource : IGuidEntity => await source.FirstOrDefaultAsync(x => x.Id == id, ct);
}
