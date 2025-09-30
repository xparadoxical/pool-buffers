#if NET9_0_OR_GREATER
namespace PoolBuffers.Tests;

public class PooledSequenceTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void RequiresSizeAtLeast1(int size)
	{
		var seq = new PooledSequence<int>(1);

		Assert.Multiple(
			() => Assert.Throws<ArgumentOutOfRangeException>(() => new PooledSequence<int>(size)),
			() => Assert.Throws<ArgumentOutOfRangeException>(() => seq.Grow(size)));
	}

	[Fact]
	public void Grow_Works()
	{
		var seq = new PooledSequence<int>(1);

		seq.Grow(3);

		var first = seq.GetSpan(0);
		var second = seq.GetSpan(first.Length);
		Assert.True(second.Length >= 3);
	}

	[Fact]
	public void GetSpan_Works_SingleSegment()
	{
		var seq = new PooledSequence<int>(1);

		Assert.True(seq.GetSpan(0).Length >= 1);
	}

	[Fact]
	public void GetSpan_Works_MultiSegment()
	{
		var seq = new PooledSequence<int>(1);
		seq.Grow(5);

		var second = seq.GetSpan(seq.Length - 5);

		Assert.True(second.Length >= 5);
	}

	[Fact]
	public void GetSpan_Throws_ForOutOfRangeIndexes()
	{
		var seq = new PooledSequence<int>(1);

		Assert.Multiple(
			() => Assert.Throws<ArgumentOutOfRangeException>(() => seq.GetSpan(seq.Length + 5)),
			() => Assert.Throws<ArgumentOutOfRangeException>(() => seq.GetSpan(-1)));
	}

	[Fact]
	public void Enumeration_Works()
	{
		var seq = new PooledSequence<int>(1);
		seq.Grow(5);

		int i = 0;
		foreach (var span in seq)
		{
			Assert.True(span.Length > 0);
			i++;
		}

		Assert.Equal(2, i);
	}
}
#endif
