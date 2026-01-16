#if NET6_0
namespace Ecng.Tests.IO;

using System.IO;
using System.Text;

/// <summary>
/// Tests for .NET 6.0 polyfills (methods added in .NET 7+).
/// </summary>
[TestClass]
public class CompatibilityTests : BaseTestClass
{
	#region Stream.ReadExactly / ReadExactlyAsync

	[TestMethod]
	public void Stream_ReadExactly_ByteArray_Success()
	{
		var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[5];
		stream.ReadExactly(buffer, 0, 5);

		buffer.AssertEqual(new byte[] { 1, 2, 3, 4, 5 });
		stream.Position.AssertEqual(5);
	}

	[TestMethod]
	public void Stream_ReadExactly_ByteArray_ThrowsOnEndOfStream()
	{
		var data = new byte[] { 1, 2, 3 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[10];
		Throws<EndOfStreamException>(() => stream.ReadExactly(buffer, 0, 10));
	}

	[TestMethod]
	public void Stream_ReadExactly_Span_Success()
	{
		var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		using var stream = new MemoryStream(data);

		Span<byte> buffer = stackalloc byte[5];
		stream.ReadExactly(buffer);

		buffer.ToArray().AssertEqual(new byte[] { 1, 2, 3, 4, 5 });
		stream.Position.AssertEqual(5);
	}

	[TestMethod]
	public void Stream_ReadExactly_Span_ThrowsOnEndOfStream()
	{
		var data = new byte[] { 1, 2, 3 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[10];
		Throws<EndOfStreamException>(() => stream.ReadExactly(buffer.AsSpan()));
	}

	[TestMethod]
	public async Task Stream_ReadExactlyAsync_ByteArray_Success()
	{
		var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[5];
		await stream.ReadExactlyAsync(buffer, 0, 5, CancellationToken);

		buffer.AssertEqual(new byte[] { 1, 2, 3, 4, 5 });
		stream.Position.AssertEqual(5);
	}

	[TestMethod]
	public async Task Stream_ReadExactlyAsync_ByteArray_ThrowsOnEndOfStream()
	{
		var data = new byte[] { 1, 2, 3 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[10];
		await ThrowsAsync<EndOfStreamException>(async () =>
			await stream.ReadExactlyAsync(buffer, 0, 10, CancellationToken));
	}

	[TestMethod]
	public async Task Stream_ReadExactlyAsync_Memory_Success()
	{
		var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[5];
		await stream.ReadExactlyAsync(buffer.AsMemory(), CancellationToken);

		buffer.AssertEqual(new byte[] { 1, 2, 3, 4, 5 });
		stream.Position.AssertEqual(5);
	}

	[TestMethod]
	public async Task Stream_ReadExactlyAsync_Memory_ThrowsOnEndOfStream()
	{
		var data = new byte[] { 1, 2, 3 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[10];
		await ThrowsAsync<EndOfStreamException>(async () =>
			await stream.ReadExactlyAsync(buffer.AsMemory(), CancellationToken));
	}

	[TestMethod]
	public async Task Stream_ReadExactlyAsync_Cancellation()
	{
		var data = new byte[] { 1, 2, 3, 4, 5 };
		using var stream = new MemoryStream(data);

		var buffer = new byte[5];
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await stream.ReadExactlyAsync(buffer, 0, 5, cts.Token));
	}

	#endregion

	#region TextReader.ReadLineAsync / ReadToEndAsync

	[TestMethod]
	public async Task TextReader_ReadLineAsync_WithCancellation_Success()
	{
		using var reader = new StringReader("Line1\nLine2\nLine3");

		var line1 = await reader.ReadLineAsync(CancellationToken);
		var line2 = await reader.ReadLineAsync(CancellationToken);
		var line3 = await reader.ReadLineAsync(CancellationToken);
		var line4 = await reader.ReadLineAsync(CancellationToken);

		line1.AssertEqual("Line1");
		line2.AssertEqual("Line2");
		line3.AssertEqual("Line3");
		line4.AssertNull();
	}

	[TestMethod]
	public async Task TextReader_ReadLineAsync_Cancellation()
	{
		using var reader = new StringReader("Line1\nLine2");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await reader.ReadLineAsync(cts.Token));
	}

	[TestMethod]
	public async Task TextReader_ReadToEndAsync_WithCancellation_Success()
	{
		var content = "Hello\nWorld\nTest";
		using var reader = new StringReader(content);

		var result = await reader.ReadToEndAsync(CancellationToken);

		result.AssertEqual(content);
	}

	[TestMethod]
	public async Task TextReader_ReadToEndAsync_Cancellation()
	{
		using var reader = new StringReader("Hello World");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await reader.ReadToEndAsync(cts.Token));
	}

	#endregion

	#region TextReader.ReadAsync / ReadBlockAsync with Memory<char>

	[TestMethod]
	public async Task TextReader_ReadAsync_Memory_Success()
	{
		var content = "Hello World";
		using var reader = new StringReader(content);

		var buffer = new char[5];
		var read = await reader.ReadAsync(buffer.AsMemory(), CancellationToken);

		read.AssertEqual(5);
		new string(buffer).AssertEqual("Hello");
	}

	[TestMethod]
	public async Task TextReader_ReadAsync_Memory_PartialRead()
	{
		var content = "Hi";
		using var reader = new StringReader(content);

		var buffer = new char[10];
		var read = await reader.ReadAsync(buffer.AsMemory(), CancellationToken);

		read.AssertEqual(2);
		new string(buffer, 0, read).AssertEqual("Hi");
	}

	[TestMethod]
	public async Task TextReader_ReadAsync_Memory_Cancellation()
	{
		using var reader = new StringReader("Hello");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var buffer = new char[5];
		await ThrowsAsync<OperationCanceledException>(async () =>
			await reader.ReadAsync(buffer.AsMemory(), cts.Token));
	}

	[TestMethod]
	public async Task TextReader_ReadBlockAsync_Memory_Success()
	{
		var content = "Hello World Test";
		using var reader = new StringReader(content);

		var buffer = new char[5];
		var read = await reader.ReadBlockAsync(buffer.AsMemory(), CancellationToken);

		read.AssertEqual(5);
		new string(buffer).AssertEqual("Hello");
	}

	[TestMethod]
	public async Task TextReader_ReadBlockAsync_Memory_ReadsUntilBufferFull()
	{
		var content = "ABCDEFGHIJ";
		using var reader = new StringReader(content);

		var buffer = new char[10];
		var read = await reader.ReadBlockAsync(buffer.AsMemory(), CancellationToken);

		read.AssertEqual(10);
		new string(buffer).AssertEqual("ABCDEFGHIJ");
	}

	[TestMethod]
	public async Task TextReader_ReadBlockAsync_Memory_Cancellation()
	{
		using var reader = new StringReader("Hello");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var buffer = new char[5];
		await ThrowsAsync<OperationCanceledException>(async () =>
			await reader.ReadBlockAsync(buffer.AsMemory(), cts.Token));
	}

	#endregion
}
#endif
