namespace PoolBuffers.Tests;
public class PooledBufferTests
{
	[Fact]
	public void EnsureCapacity_Works()
	{
		var buf = new PooledBuffer<int>(1);
		var minCapacity = 6;

		var capacity = buf.Capacity;
		buf.EnsureCapacity(minCapacity);
		var newCapacity = buf.Capacity;

		Assert.True(newCapacity >= minCapacity);
	}

	[Fact]
	public void EnsureCapacity_Throws_ForNegativeArgs()
	{
		var buf = new PooledBuffer<int>(1);

		Assert.Throws<ArgumentOutOfRangeException>(() => buf.EnsureCapacity(-5));
	}
}
