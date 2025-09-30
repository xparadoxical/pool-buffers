namespace PoolBuffers.Tests;
public class PooledBufferTests
{
	[Fact]
	public void Grow_Works()
	{
		var buf = new PooledBuffer<int>(1);

		var length = buf.Capacity;
		buf.Grow(5);
		var newLength = buf.Capacity;

		Assert.True(newLength >= length + 5);
	}

	[Fact]
	public void Grow_Throws_ForNegativeLengths()
	{
		var buf = new PooledBuffer<int>(1);

		Assert.Throws<ArgumentOutOfRangeException>(() => buf.Grow(-5));
	}
}
