using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace PoolBuffers;

/// <summary>Growable buffer, backed by an array from <see cref="ArrayPool{T}"/>.</summary>
public sealed class PooledBuffer<T>(int minCapacity) : IEnumerable<T>, IDisposable
{
	private T[] _array = ArrayPool<T>.Shared.Rent(minCapacity);

	public PooledBuffer(ReadOnlySpan<T> initialData) : this(initialData.Length)
	{
		initialData.CopyTo(_array);
		DataLength = initialData.Length;
	}

	public Span<T> Span => _array.AsSpan();

	public ref T this[int i] => ref _array[i];

	public Span<T> this[Range r] => _array[r];

	public int Capacity => _array.Length;

	public int DataLength { get; set; }

	public Span<T> DataSpan => _array.AsSpan(0, DataLength);

	public void Grow(int byAtLeast)
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
