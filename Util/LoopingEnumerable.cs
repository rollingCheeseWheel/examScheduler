using System.Collections;

namespace Util;

public class LoopingEnumerable<T>(IList<T> items, int maxIterations = -1) : IEnumerable<T>
{
	private readonly IList<T> _items = items;
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

		while (remaining != 0)
		{
			yield return _items[ index ];
			index = ( index + 1 ) % _items.Count;

			if (remaining > 0)
			{
				remaining--;
			}
		}
	}
}
