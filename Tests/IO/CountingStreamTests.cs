namespace Ecng.Tests.IO;

using Ecng.IO;

[TestClass]
public class CountingStreamTests : BaseTestClass
{
	[TestMethod]
	public void UnderlyingIsRequired()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new CountingStream(null));
	}

	[TestMethod]
	public void NothingCarriedIsNothingCounted()
	{
		using var ms = new MemoryStream([1, 2, 3]);
		using var counting = new CountingStream(ms);

		counting.ReadCount.AssertEqual(0L);
		counting.WriteCount.AssertEqual(0L);
	}

	[TestMethod]
	public void EveryShapeOfReadIsCounted()
	{
		using var ms = new MemoryStream(new byte[64]);
		using var counting = new CountingStream(ms);

		counting.Read(new byte[4], 0, 4);
		counting.ReadCount.AssertEqual(4L);

		counting.Read(new byte[6].AsSpan());
		counting.ReadCount.AssertEqual(10L);

		counting.ReadByte();
		counting.ReadCount.AssertEqual(11L);
	}

	[TestMethod]
	public async Task EveryShapeOfAsyncReadIsCounted()
	{
		using var ms = new MemoryStream(new byte[64]);
		using var counting = new CountingStream(ms);

		await counting.ReadAsync(new byte[8], 0, 8, CancellationToken.None);
		counting.ReadCount.AssertEqual(8L);

		await counting.ReadAsync(new byte[3].AsMemory(), CancellationToken.None);
		counting.ReadCount.AssertEqual(11L);
	}

	[TestMethod]
	public void EveryShapeOfWriteIsCounted()
	{
		using var ms = new MemoryStream();
		using var counting = new CountingStream(ms);

		counting.Write([1, 2, 3], 0, 3);
		counting.WriteCount.AssertEqual(3L);

		counting.Write(new byte[2].AsSpan());
		counting.WriteCount.AssertEqual(5L);

		counting.WriteByte(7);
		counting.WriteCount.AssertEqual(6L);

		counting.ReadCount.AssertEqual(0L, "what goes out is not what comes in");
	}

	[TestMethod]
	public async Task EveryShapeOfAsyncWriteIsCounted()
	{
		using var ms = new MemoryStream();
		using var counting = new CountingStream(ms);

		await counting.WriteAsync(new byte[5], 0, 5, CancellationToken.None);
		counting.WriteCount.AssertEqual(5L);

		await counting.WriteAsync(new byte[2].AsMemory(), CancellationToken.None);
		counting.WriteCount.AssertEqual(7L);
	}

	[TestMethod]
	public void TheEndOfTheStreamAddsNothing()
	{
		using var ms = new MemoryStream([1, 2]);
		using var counting = new CountingStream(ms);

		counting.Read(new byte[8], 0, 8).AssertEqual(2);
		counting.ReadCount.AssertEqual(2L);

		// A read that handed nothing over carried nothing.
		counting.Read(new byte[8], 0, 8).AssertEqual(0);
		counting.ReadByte().AssertEqual(-1);
		counting.ReadCount.AssertEqual(2L);
	}

	[TestMethod]
	public void WhatIsCarriedReachesTheUnderlyingStream()
	{
		var ms = new MemoryStream();

		using (var counting = new CountingStream(ms, leaveOpen: true))
		{
			counting.Write([1, 2, 3], 0, 3);
			counting.Flush();
		}

		ms.ToArray().AssertEqual([(byte)1, (byte)2, (byte)3]);
	}

	[TestMethod]
	public void DisposingClosesTheUnderlyingStreamUnlessLeftOpen()
	{
		var closed = new MemoryStream();
		new CountingStream(closed).Dispose();
		Assert.ThrowsExactly<ObjectDisposedException>(() => closed.ReadByte());

		var kept = new MemoryStream([1]);
		new CountingStream(kept, leaveOpen: true).Dispose();
		kept.ReadByte().AssertEqual(1);
	}
}
