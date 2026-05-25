using System.Collections;

namespace Util.DataStructures;

public class LoopingEnumerable
{
	public static LoopingEnumerable<TSource> From<TSource>(IEnumerable<TSource> items, int maxIterations = byte.MaxValue) => new(items, maxIterations);
}

public class LoopingEnumerable<T>(IEnumerable<T> items, int maxIterations = byte.MaxValue) : LoopingEnumerable, IEnumerable<T>
{
	private readonly List<T> _items = items.ToList();
	private readonly int _maxIterations = maxIterations;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<T> GetEnumerator()
	{
		if (_items.Count == 0)
		{
			yield break;
		}

		var index = 0;
		var remaining = _maxIterations;

		while (remaining > 1)
		{
			yield return _items[ index ];
			index = ( index + 1 ) % _items.Count;
			remaining--;
		}
	}
}
