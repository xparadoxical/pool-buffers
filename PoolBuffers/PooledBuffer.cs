using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace PoolBuffers;

/// <summary>Growable buffer, backed by an array from <see cref="ArrayPool{T}"/>.</summary>
public sealed class PooledBuffer<T>(int minSize) : IEnumerable<T>, IDisposable
{
	private T[] _array = ArrayPool<T>.Shared.Rent(minSize);

	public Span<T> Span => _array.AsSpan();

	public int Length
	{
		get => _array.Length;
		set => Grow(value - _array.Length);
	}

	public void Grow(int byAtLeast)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(byAtLeast);

		var newArray = ArrayPool<T>.Shared.Rent(_array.Length + byAtLeast);
		_array.AsSpan().CopyTo(newArray);
		Dispose();
		_array = newArray;
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _array.GetEnumerator();

	/// <summary>Returns the array to the pool, clearing it when <typeparamref name="T"/> is not <see langword="unmanaged"/>.</summary>
	public void Dispose()
	{
		ArrayPool<T>.Shared.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
		_array = null!;
	}
}
