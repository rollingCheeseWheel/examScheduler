using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System.Linq.Expressions;

namespace Util.Extensions;

public interface IGuidEntity
{
	Guid Id { get; }
}

public static class LinqExtensions
{
	public static bool ValueEquals<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
		where TSource : IComparable<TSource> => ValueEquals(first, second, x => x);

	public static bool ValueEquals<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> selector)
		where TKey : IComparable<TKey> => first.Select(selector).Order().SequenceEqual(second.Select(selector).Order());

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

	public static ICollection<TSource> AddRange<TSource>(this ICollection<TSource> source, params TSource[ ] items) => source.AddRange(items.ToList());
	public static ICollection<TSource> AddRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> items)
	{
		foreach (var item in items)
		{
			source.Add(item);
		}
		return source;
	}

	public static ICollection<TSource> RemoveRange<TSource>(this ICollection<TSource> source, params TSource[ ] items) => source.RemoveRange(items.ToList());

	public static ICollection<TSource> RemoveRange<TSource>(this ICollection<TSource> source, IEnumerable<TSource> items)
	{
		foreach (var item in items)
		{
			source.Remove(item);
		}
		return source;
	}

	public static IEnumerable<TResult> DistinctMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector) where TResult : IEquatable<TResult> => source.SelectMany(selector).Distinct();

	public static IQueryable<TSource> WhereId<TSource>(this IQueryable<TSource> source, Guid id) where TSource : IGuidEntity => source.Where(x => x.Id == id);

	public static IEnumerable<TSource> WhereId<TSource>(this IEnumerable<TSource> source, Guid id) where TSource : IGuidEntity => source.Where(x => x.Id == id);

	public static IQueryable<TSource> WhereId<TSource, TItem>(this IQueryable<TSource> source, Expression<Func<TSource, TItem>> selector, Guid id) where TItem : IGuidEntity
	{
		var parameter = selector.Parameters[ 0 ];

		var body = Expression.Equal(
			Expression.Property(selector.Body, nameof(IGuidEntity.Id)),
			Expression.Constant(id)
		);

		var predicate = Expression.Lambda<Func<TSource, bool>>(body, parameter);

		return source.Where(predicate);
	}

	public static IEnumerable<TSource> WhereId<TSource, TItem>(this IEnumerable<TSource> source, Func<TSource, TItem> selector, Guid id) where TItem : IGuidEntity => source.Where(x => selector(x).Id == id);

	public static TSource? FindById<TSource>(this IEnumerable<TSource> source, Guid id) where TSource : IGuidEntity => source.FirstOrDefault(x => x.Id == id);

	public static async Task<TSource?> FindByIdAsync<TSource>(this IQueryable<TSource> source, Guid id, CancellationToken ct = default) where TSource : IGuidEntity => await source.FirstOrDefaultAsync(x => x.Id == id, ct);

	public static async Task<IDictionary<Guid, TSource>> FindByIdAsync<TSource>(this IQueryable<TSource> source, ICollection<Guid> ids, CancellationToken ct = default) where TSource : IGuidEntity
	{
		var entities = await source.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
		return entities.Count == ids.Count ? entities : [ ];
	}

	public static IQueryable<TSource> WhereNotNull<TSource>(this IQueryable<TSource?> source) => source.Where(x => x != null).Cast<TSource>();

	public static IQueryable<TSource> WhereNotNull<TSource, TKey>(
		this IQueryable<TSource?> source,
		Expression<Func<TSource, TKey>> selector
	)
	{
		var param = selector.Parameters[ 0 ];
		var body = Expression.NotEqual(
			selector.Body,
			Expression.Constant(null, typeof(TKey))
		);

		var predicate = Expression.Lambda<Func<TSource?, bool>>(body, param);
		return source.Where(predicate).Cast<TSource>();
	}

	public static IQueryable<TResult> JoinOnId<TOuter, TInner, TResult>(this IQueryable<TOuter> outer, IQueryable<TInner> inner, Expression<Func<TOuter, Guid>> outerSelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
		where TOuter : IGuidEntity
		where TInner : IGuidEntity
		=> outer.Join(inner, outerSelector, x => x.Id, resultSelector);

	public static IQueryable<TInner> JoinInnerOnId<TOuter, TInner>(this IQueryable<TOuter> outer, IQueryable<TInner> inner, Expression<Func<TOuter, Guid>> outerSelector)
		where TOuter : IGuidEntity
		where TInner : IGuidEntity
		=> outer.Join(inner, outerSelector, x => x.Id, (o, i) => i);

	public static IEnumerable<TSource> DistinctById<TSource>(this IEnumerable<TSource> source) where TSource : IGuidEntity => source.DistinctBy(x => x.Id);

	public static IQueryable<TSource> DistinctById<TSource>(this IQueryable<TSource> source) where TSource : IGuidEntity => source.DistinctBy(x => x.Id);

	public static IQueryable<TSource> OrderById<TSource>(this IQueryable<TSource> source) where TSource : IGuidEntity => source.OrderBy(x => x.Id);

	public static IEnumerable<TSource> OrderById<TSource>(this IEnumerable<TSource> source) where TSource : IGuidEntity => source.OrderBy(x => x.Id);
}
