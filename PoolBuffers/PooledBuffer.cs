using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace PoolBuffers;

/// <summary>
/// Growable buffer backed by an array from <see cref="ArrayPool{T}"/>.
/// Includes manual length tracking support.
/// </summary>
public sealed class PooledBuffer<T>(int minCapacity) : IEnumerable<T>, IDisposable
{
	private T[] _array = ArrayPool<T>.Shared.Rent(minCapacity);

	/// <summary>Initializes the buffer and its length with <paramref name="initialData"/>.</summary>
	public PooledBuffer(ReadOnlySpan<T> initialData) : this(initialData.Length)
	{
		initialData.CopyTo(_array);
		Length = initialData.Length;
	}

	/// <summary>Span representing the entire buffer.</summary>
	public Span<T> Span => _array.AsSpan();

	public ref T this[int i] => ref _array[i];

	public Span<T> this[Range r] => _array[r];

	/// <summary>The number of elements this buffer can hold.</summary>
	public int Capacity => _array.Length;

	/// <summary>Length of the valid area of the buffer.</summary>
	public int Length { get; set; }

	/// <summary>Span to the valid area of the buffer, located at the start of the backing array.</summary>
	public Span<T> DataSpan => _array.AsSpan(0, Length);

	private void Grow(int byAtLeast)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(byAtLeast);

		var newArray = ArrayPool<T>.Shared.Rent(_array.Length + byAtLeast);
		_array.AsSpan().CopyTo(newArray);
		Dispose();
		_array = newArray;
	}

	/// <summary>Resizes the buffer if it's smaller than <paramref name="minCapacity"/>.</summary>
	public void EnsureCapacity(int minCapacity)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minCapacity);

		if (_array.Length < minCapacity)
			Grow(minCapacity - _array.Length);
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
