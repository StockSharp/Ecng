namespace Ecng.Tests.IO;

using System.IO.Compression;
using System.Text;

using Ecng.IO;

[TestClass]
public class CompressTests : BaseTestClass
{
	[TestMethod]
	public void Deflate()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		bytes.DeflateTo().DeflateFrom().AssertEqual(bytes);
		bytes.Compress<DeflateStream>().DeflateFrom().AssertEqual(bytes);
		bytes.Compress<DeflateStream>().Uncompress<DeflateStream>().AssertEqual(bytes);
	}

	[TestMethod]
	public void GZip()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		bytes.Compress<GZipStream>().Uncompress<GZipStream>().AssertEqual(bytes);
	}

	[TestMethod]
	public void GZipStr()
	{
		var str = "Hello world";
		str.UTF8().Compress<GZipStream>().UnGZip().AssertEqual(str);
	}

	[TestMethod]
	public void GZipToBuffer()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var rangeCount = 100;
		var destination = new byte[rangeCount * 2];
		var range = bytes.Compress<GZipStream>(count: rangeCount);
		var count = range.UnGZip(0, range.Length, destination);
		count.AssertEqual(rangeCount);
		bytes.Take(count).SequenceEqual(destination.Take(rangeCount)).AssertTrue();
	}

	[TestMethod]
	public void UnDeflateToBuffer()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var rangeCount = 100;
		var destination = new byte[rangeCount * 2];
		var range = bytes.Compress<DeflateStream>(count: rangeCount);
		var count = range.UnDeflate(0, range.Length, destination);
		count.AssertEqual(rangeCount);
		bytes.Take(count).SequenceEqual(destination.Take(rangeCount)).AssertTrue();
	}

	[TestMethod]
	public void Zip7()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		// TODO 7Zip compression not implemented
		//bytes.Do7Zip().Un7Zip().SequenceEqual(bytes).AssertTrue();
		//bytes.Compress<Lzma.LzmaStream>().Uncompress<Lzma.LzmaStream>().SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public async Task Async()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		var token = CancellationToken;

		async Task Do<TCompress>()
			where TCompress : Stream
			=> (await (await bytes.CompressAsync<TCompress>(cancellationToken: token)).UncompressAsync<TCompress>(cancellationToken: token)).SequenceEqual(bytes).AssertTrue();

		await Do<DeflateStream>();
		await Do<GZipStream>();
		//await Do<Lzma.LzmaStream>();
	}

#if !NETSTANDARD2_0
	[TestMethod]
	public void GZipSpan()
	{
		var str = "Hello world from Span test! This is a longer string to compress and decompress using ReadOnlySpan.";
		var compressed = str.UTF8().Compress<GZipStream>();

		// Test UnGZip(ReadOnlySpan<byte>)
		ReadOnlySpan<byte> compressedSpan = compressed;
		var decompressed = compressedSpan.UnGZip();
		decompressed.AssertEqual(str);
	}

	[TestMethod]
	public void GZipSpanToBuffer()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var rangeCount = 100;
		var compressed = bytes.Compress<GZipStream>(count: rangeCount);
		var destination = new byte[rangeCount * 2];

		// Test UnGZip(ReadOnlySpan<byte>, Span<byte>)
		ReadOnlySpan<byte> compressedSpan = compressed;
		var count = compressedSpan.UnGZip(destination);
		count.AssertEqual(rangeCount);
		bytes.Take(count).SequenceEqual(destination.Take(rangeCount)).AssertTrue();
	}

	[TestMethod]
	public void DeflateSpan()
	{
		var str = "Testing Deflate compression with ReadOnlySpan! This should work properly now.";
		var compressed = str.UTF8().Compress<DeflateStream>();

		// Test UnDeflate(ReadOnlySpan<byte>)
		ReadOnlySpan<byte> compressedSpan = compressed;
		var decompressed = compressedSpan.UnDeflate();
		decompressed.AssertEqual(str);
	}

	[TestMethod]
	public void DeflateSpanToBuffer()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var rangeCount = 100;
		var compressed = bytes.Compress<DeflateStream>(count: rangeCount);
		var destination = new byte[rangeCount * 2];

		// Test UnDeflate(ReadOnlySpan<byte>, Span<byte>)
		ReadOnlySpan<byte> compressedSpan = compressed;
		var count = compressedSpan.UnDeflate(destination);
		count.AssertEqual(rangeCount);
		bytes.Take(count).SequenceEqual(destination.Take(rangeCount)).AssertTrue();
	}

	[TestMethod]
	public void DeflateFromSpan()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var compressed = bytes.DeflateTo();

		// Test DeflateFrom(ReadOnlySpan<byte>)
		ReadOnlySpan<byte> compressedSpan = compressed;
		var decompressed = compressedSpan.DeflateFrom();
		decompressed.SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public async Task CompressMemoryAsync()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);
		var token = CancellationToken;

		// Test CompressAsync(ReadOnlyMemory<byte>)
		ReadOnlyMemory<byte> bytesMemory = bytes;
		var compressed = await bytesMemory.CompressAsync<GZipStream>(cancellationToken: token);
		var decompressed = await compressed.UncompressAsync<GZipStream>(cancellationToken: token);
		decompressed.SequenceEqual(bytes).AssertTrue();

		// Test with DeflateStream
		var compressedDeflate = await bytesMemory.CompressAsync<DeflateStream>(cancellationToken: token);
		var decompressedDeflate = await compressedDeflate.UncompressAsync<DeflateStream>(cancellationToken: token);
		decompressedDeflate.SequenceEqual(bytes).AssertTrue();
	}
#endif

	[TestMethod]
	public void UsableEntry()
	{
		// Build a simple in-memory zip with two files
		var ms = new MemoryStream();

		using (var a = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
		{
			void Add(string name, string text)
			{
				var entry = a.CreateEntry(name);
				using var es = entry.Open();
				var data = text.UTF8();
				es.Write(data, 0, data.Length);
			}

			Add("a.txt", "hello");
			Add("b.txt", "world");
		}

		ms.Position = 0;

		string ReadAll(Stream s)
		{
			using var sr = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
			return sr.ReadToEnd();
		}

		using var zip = ms.Unzip(leaveOpen: true);

		// Materialize results first (typical pattern), then consume streams later
		var entries = zip.ToArray();

		entries.Length.AssertEqual(2);

		entries[0].name.EndsWith("a.txt").AssertTrue();
		ReadAll(entries[0].body).AssertEqual("hello");
		ReadAll(entries[0].body).AssertEqual(string.Empty);

		entries[1].name.EndsWith("b.txt").AssertTrue();
		ReadAll(entries[1].body).AssertEqual("world");
		ReadAll(entries[1].body).AssertEqual(string.Empty);
	}

	[TestMethod]
	public async Task AsyncCompress()
	{
		var inputBytes = RandomGen.GetBytes(FileSizes.KB);
		using var input = new TestInputStream(inputBytes);
		using var output = new MemoryStream();

		await input.CompressAsync<TestCompressStream>(output, leaveOpen: true, cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task AsyncUncompress()
	{
		var inputBytes = RandomGen.GetBytes(FileSizes.KB);
		using var input = new MemoryStream(inputBytes);
		using var output = new MemoryStream();

		await input.UncompressAsync<TestDecompressStream>(output, leaveOpen: true, cancellationToken: CancellationToken);
	}

	private sealed class TestInputStream(byte[] data) : Stream
	{
		private readonly MemoryStream _ms = new(data);

        public override bool CanRead => _ms.CanRead;
		public override bool CanSeek => _ms.CanSeek;
		public override bool CanWrite => false;
		public override long Length => _ms.Length;
		public override long Position { get => _ms.Position; set => _ms.Position = value; }

		public override void Flush() => _ms.Flush();
		public override Task FlushAsync(CancellationToken cancellationToken) => _ms.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count) => _ms.Read(buffer, offset, count);
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _ms.ReadAsync(buffer, offset, count, cancellationToken);

		public override long Seek(long offset, SeekOrigin origin) => _ms.Seek(offset, origin);
		public override void SetLength(long value) => _ms.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		// Override CopyToAsync to force asynchronous behavior and delay between chunks
		public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			var buffer = new byte[bufferSize];
			while (true)
			{
				var read = await ReadAsync(buffer, 0, buffer.Length, cancellationToken).NoWait();
				if (read == 0)
					break;
				// Small delay to ensure method returns before all writes complete
				await Task.Delay(50, cancellationToken).NoWait();
				await destination.WriteAsync(buffer, 0, read, cancellationToken).NoWait();
			}
		}
	}

	// Helper test stream that simulates delayed async writes and fails if disposed early
	private sealed class TestCompressStream(Stream output, CompressionLevel level, bool leaveOpen) : Stream
	{
		private readonly Stream _output = output ?? throw new ArgumentNullException(nameof(output));
		private readonly bool _leaveOpen = leaveOpen;
		private bool _disposed;

        public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Flush() => _output.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) => _output.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count)
		{
			// Prevent synchronous write path; force CopyToAsync to use WriteAsync
			throw new NotSupportedException();
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			// Small delay to let method return to caller and allow premature dispose
			await Task.Delay(50, cancellationToken).NoWait();
			if (_disposed)
				throw new ObjectDisposedException(nameof(TestCompressStream));
			await _output.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).NoWait();
		}

		protected override void Dispose(bool disposing)
		{
			_disposed = true;
			if (disposing && !_leaveOpen)
				_output.Dispose();
			base.Dispose(disposing);
		}
	}

	// Helper test stream that simulates delayed async reads and fails if disposed early
	private sealed class TestDecompressStream(Stream input, CompressionMode mode, bool leaveOpen) : Stream
	{
		private readonly Stream _input = input ?? throw new ArgumentNullException(nameof(input));
		private readonly bool _leaveOpen = leaveOpen;
		private bool _disposed;

        public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Flush() => _input.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) => _input.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count)
		{
			return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			// Small delay to let method return to caller and allow premature dispose
			await Task.Delay(50, cancellationToken).NoWait();
			if (_disposed)
				throw new ObjectDisposedException(nameof(TestDecompressStream));
			return await _input.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).NoWait();
		}

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		// Override CopyToAsync to be asynchronous and delayed to reproduce premature Dispose
		public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			var buffer = new byte[bufferSize];
			while (true)
			{
				var read = await ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).NoWait();
				if (read == 0)
					break;
				// Small delay to ensure method returns before all writes complete
				await Task.Delay(50, cancellationToken).NoWait();
				if (_disposed)
					throw new ObjectDisposedException(nameof(TestDecompressStream));
				await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).NoWait();
			}
		}

		protected override void Dispose(bool disposing)
		{
			_disposed = true;
			if (disposing && !_leaveOpen)
				_input.Dispose();
			base.Dispose(disposing);
		}
	}
}