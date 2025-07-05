using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace PoolBuffers;

internal class SequenceSegment<T>(T[] array)
{
	public T[] Array { get; } = array;
	public int Length => Array.Length;
	public SequenceSegment<T>? Next { get; internal set; } = null;
	public long RunningIndex { get; internal set; } = 0;
}

/// <summary>
/// Growable sequence of items, backed by multiple arrays from <see cref="ArrayPool{T}"/>.
/// </summary>
public struct PooledSequence<T> : IEnumerable<Span<T>>, IDisposable
{
	/// <summary>Always has at least one item.</summary>
	private readonly SequenceSegment<T> _first;
	private SequenceSegment<T> _last;

	public readonly long Length => _last.RunningIndex + _last.Length;

	/// <summary>Initializes the sequence with a single array.</summary>
	/// <param name="minInitialSize">Minimum size of the first array. Must be at least 1.</param>
	public PooledSequence(int minInitialSize)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(minInitialSize, 1);

		_first = _last = new(ArrayPool<T>.Shared.Rent(minInitialSize));
	}

	/// <summary>Rents another array and adds it to the sequence.</summary>
	/// <param name="minSize">Minimum size of the next array. Must be at least 1.</param>
	public void Grow(int minSize)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(minSize, 1);

		_last.Next = new(ArrayPool<T>.Shared.Rent(minSize))
		{
			RunningIndex = _last.RunningIndex + _last.Length
		};
		_last = _last.Next;
	}

	/// <summary>Gets a span to the array that's at the specified element index in the sequence.</summary>
	/// <param name="elementIndex">Index of an element of the sequence.</param>
	/// <returns>A span at least <paramref name="elementIndex"/> items long.</returns>
	public readonly Span<T> GetSpan(long elementIndex)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(elementIndex, _last.RunningIndex + _last.Length);
		ArgumentOutOfRangeException.ThrowIfNegative(elementIndex);

		var segment = _first;
		while (true)
		{
			if (elementIndex - segment.RunningIndex < segment.Length) //ri <= i < ri+l  <=>  0 <= i-ri < l
				return segment.Array.AsSpan();

			if (segment.Next is not SequenceSegment<T> next)
				break;
			segment = next;
		}

		throw new UnreachableException();
	}

	/// <summary>Returns all arrays to the pool.</summary>
	public readonly void Dispose()
	{
		var segment = _first;
		while (true)
		{
			ArrayPool<T>.Shared.Return(segment.Array, true);

			if (segment.Next is not SequenceSegment<T> next)
				break;
			segment = next;
		}
	}

	public readonly IEnumerator<Span<T>> GetEnumerator() => new SequenceEnumerator(_first);

	readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	class SequenceEnumerator(SequenceSegment<T> current) : IEnumerator<Span<T>>
	{
		public Span<T> Current => current.Array.AsSpan();

		public bool MoveNext()
		{
			if (current.Next is not SequenceSegment<T> next)
				return false;

			current = next;
			return true;
		}

		public void Dispose() => current = null!;

		object IEnumerator.Current => throw new NotSupportedException("Can't box a Span.");
		void IEnumerator.Reset() => throw new NotSupportedException();
	}
}
