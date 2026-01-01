namespace Ecng.Tests.IO;

using System.IO.Compression;
using System.Text;

using Ecng.IO;
using Ecng.IO.Compression;

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
	public void Zip_CreatesArchiveFromEntries()
	{
		var entries = new List<(string name, Stream body)>
		{
			("file1.txt", new MemoryStream("content1"u8.ToArray())),
			("subdir/file2.txt", new MemoryStream("content2"u8.ToArray()))
		};

		using var zipStream = new MemoryStream();
		entries.Zip(zipStream);
		zipStream.Position = 0;

		// Verify the archive contains expected entries
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
		archive.Entries.Count.AssertEqual(2);

		var entry1 = archive.GetEntry("file1.txt");
		(entry1 != null).AssertTrue();
		using (var reader = new StreamReader(entry1.Open()))
			reader.ReadToEnd().AssertEqual("content1");

		var entry2 = archive.GetEntry("subdir/file2.txt");
		(entry2 != null).AssertTrue();
		using (var reader = new StreamReader(entry2.Open()))
			reader.ReadToEnd().AssertEqual("content2");
	}

	[TestMethod]
	public async Task ZipAsync_CreatesArchiveFromEntries()
	{
		var entries = new List<(string name, Stream body)>
		{
			("async1.txt", new MemoryStream("async content 1"u8.ToArray())),
			("async2.txt", new MemoryStream("async content 2"u8.ToArray()))
		};

		using var zipStream = new MemoryStream();
		await entries.ZipAsync(zipStream, cancellationToken: CancellationToken);
		zipStream.Position = 0;

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
		archive.Entries.Count.AssertEqual(2);
	}

	[TestMethod]
	public void Zip_Unzip_RoundTrip()
	{
		var original = new Dictionary<string, string>
		{
			["a.txt"] = "Hello",
			["dir/b.txt"] = "World",
			["dir/sub/c.txt"] = "Test"
		};

		var entries = original.Select(kv => (kv.Key, (Stream)new MemoryStream(kv.Value.UTF8()))).ToList();

		using var zipStream = new MemoryStream();
		entries.Zip(zipStream);
		zipStream.Position = 0;

		using var unzipped = zipStream.Unzip(leaveOpen: true);

		foreach (var (name, body) in unzipped)
		{
			using var reader = new StreamReader(body);
			var content = reader.ReadToEnd();
			original[name].AssertEqual(content);
		}
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
#pragma warning disable CS9113 // Parameter is unread.
	private sealed class TestCompressStream(Stream output, CompressionLevel level, bool leaveOpen) : Stream
#pragma warning restore CS9113 // Parameter is unread.
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
#pragma warning disable CS9113 // Parameter is unread.
	private sealed class TestDecompressStream(Stream input, CompressionMode mode, bool leaveOpen) : Stream
#pragma warning restore CS9113 // Parameter is unread.
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

	#region FileSystem Zip Extensions Tests

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_ZipFrom_UnzipTo_RoundTrip(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// Create source directory with files
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");

		var subDir = Path.Combine(sourceDir, "subdir");
		fs.CreateDirectory(subDir);
		fs.WriteAllText(Path.Combine(subDir, "file2.txt"), "content2");

		// Create zip from directory
		var zipPath = Path.Combine(root, "archive.zip");
		fs.ZipFrom(sourceDir, zipPath);

		fs.FileExists(zipPath).AssertTrue();

		// Unzip to new directory
		var destDir = Path.Combine(root, "dest");
		fs.UnzipTo(zipPath, destDir);

		// Verify extracted files
		fs.FileExists(Path.Combine(destDir, "file1.txt")).AssertTrue();
		fs.ReadAllText(Path.Combine(destDir, "file1.txt")).AssertEqual("content1");

		fs.FileExists(Path.Combine(destDir, "subdir", "file2.txt")).AssertTrue();
		fs.ReadAllText(Path.Combine(destDir, "subdir", "file2.txt")).AssertEqual("content2");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task FileSystem_ZipFromAsync_UnzipToAsync_RoundTrip(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "async1.txt"), "async content 1");
		fs.WriteAllText(Path.Combine(sourceDir, "async2.txt"), "async content 2");

		var zipPath = Path.Combine(root, "async_archive.zip");
		await fs.ZipFromAsync(sourceDir, zipPath, cancellationToken: CancellationToken);

		fs.FileExists(zipPath).AssertTrue();

		var destDir = Path.Combine(root, "dest");
		await fs.UnzipToAsync(zipPath, destDir, cancellationToken: CancellationToken);

		fs.ReadAllText(Path.Combine(destDir, "async1.txt")).AssertEqual("async content 1");
		fs.ReadAllText(Path.Combine(destDir, "async2.txt")).AssertEqual("async content 2");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_Zip_FromEntries(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var zipPath = Path.Combine(root, "entries.zip");

		var entries = new List<(string name, Stream body)>
		{
			("entry1.txt", new MemoryStream("entry content 1"u8.ToArray())),
			("dir/entry2.txt", new MemoryStream("entry content 2"u8.ToArray()))
		};

		fs.Zip(zipPath, entries);

		fs.FileExists(zipPath).AssertTrue();

		// Verify by unzipping
		using var zipEntries = fs.Unzip(zipPath);
		var list = zipEntries.ToList();

		list.Count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_Unzip_ReturnsEntries(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// Create a zip file first
		var zipPath = Path.Combine(root, "test.zip");
		var entries = new List<(string name, Stream body)>
		{
			("a.txt", new MemoryStream("aaa"u8.ToArray())),
			("b.txt", new MemoryStream("bbb"u8.ToArray()))
		};
		fs.Zip(zipPath, entries);

		// Test Unzip extension
		using var unzipped = fs.Unzip(zipPath);
		var list = unzipped.ToList();

		list.Count.AssertEqual(2);
		list.Any(e => e.name == "a.txt").AssertTrue();
		list.Any(e => e.name == "b.txt").AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_UnzipTo_OverwriteOption(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// Create initial zip
		var zipPath = Path.Combine(root, "overwrite.zip");
		var entries = new List<(string name, Stream body)>
		{
			("file.txt", new MemoryStream("new content"u8.ToArray()))
		};
		fs.Zip(zipPath, entries);

		// Create dest with existing file
		var destDir = Path.Combine(root, "dest");
		fs.CreateDirectory(destDir);
		fs.WriteAllText(Path.Combine(destDir, "file.txt"), "old content");

		// Unzip with overwrite=true (default)
		fs.UnzipTo(zipPath, destDir, overwrite: true);
		fs.ReadAllText(Path.Combine(destDir, "file.txt")).AssertEqual("new content");

		// Reset
		fs.WriteAllText(Path.Combine(destDir, "file.txt"), "old content");

		// Unzip with overwrite=false
		fs.UnzipTo(zipPath, destDir, overwrite: false);
		fs.ReadAllText(Path.Combine(destDir, "file.txt")).AssertEqual("old content");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_Zip_ToStream(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var entries = new List<(string name, Stream body)>
		{
			("stream1.txt", new MemoryStream("stream content 1"u8.ToArray())),
			("stream2.txt", new MemoryStream("stream content 2"u8.ToArray()))
		};

		using var zipStream = new MemoryStream();
		fs.Zip(zipStream, entries);
		zipStream.Position = 0;

		// Verify archive
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
		archive.Entries.Count.AssertEqual(2);
		archive.Entries.Any(e => e.Name == "stream1.txt").AssertTrue();
		archive.Entries.Any(e => e.Name == "stream2.txt").AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task FileSystem_ZipAsync_ToStream(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var entries = new List<(string name, Stream body)>
		{
			("async_stream1.txt", new MemoryStream("async stream 1"u8.ToArray())),
			("async_stream2.txt", new MemoryStream("async stream 2"u8.ToArray()))
		};

		using var zipStream = new MemoryStream();
		await fs.ZipAsync(zipStream, entries, cancellationToken: CancellationToken);
		zipStream.Position = 0;

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
		archive.Entries.Count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task FileSystem_ZipAsync_FromEntries(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var zipPath = Path.Combine(root, "async_entries.zip");

		var entries = new List<(string name, Stream body)>
		{
			("async1.txt", new MemoryStream("async content 1"u8.ToArray())),
			("dir/async2.txt", new MemoryStream("async content 2"u8.ToArray()))
		};

		await fs.ZipAsync(zipPath, entries, cancellationToken: CancellationToken);

		fs.FileExists(zipPath).AssertTrue();

		using var zipEntries = fs.Unzip(zipPath);
		var list = zipEntries.ToList();
		list.Count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_ZipFrom_ToStream(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");
		fs.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "content2");

		using var zipStream = new MemoryStream();
		fs.ZipFrom(sourceDir, zipStream);
		zipStream.Position = 0;

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
		archive.Entries.Count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task FileSystem_ZipFromAsync_ToStream(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");

		using var zipStream = new MemoryStream();
		await fs.ZipFromAsync(sourceDir, zipStream, cancellationToken: CancellationToken);
		zipStream.Position = 0;

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
		archive.Entries.Count.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_UnzipTo_FromStream(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// Create zip in memory
		var entries = new List<(string name, Stream body)>
		{
			("fromstream.txt", new MemoryStream("from stream content"u8.ToArray()))
		};

		using var zipStream = new MemoryStream();
		entries.Zip(zipStream);
		zipStream.Position = 0;

		// Unzip from stream
		var destDir = Path.Combine(root, "dest");
		fs.UnzipTo(zipStream, destDir);

		fs.FileExists(Path.Combine(destDir, "fromstream.txt")).AssertTrue();
		fs.ReadAllText(Path.Combine(destDir, "fromstream.txt")).AssertEqual("from stream content");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task FileSystem_UnzipToAsync_FromStream(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var entries = new List<(string name, Stream body)>
		{
			("async_fromstream.txt", new MemoryStream("async from stream"u8.ToArray()))
		};

		using var zipStream = new MemoryStream();
		entries.Zip(zipStream);
		zipStream.Position = 0;

		var destDir = Path.Combine(root, "dest");
		await fs.UnzipToAsync(zipStream, destDir, cancellationToken: CancellationToken);

		fs.FileExists(Path.Combine(destDir, "async_fromstream.txt")).AssertTrue();
		fs.ReadAllText(Path.Combine(destDir, "async_fromstream.txt")).AssertEqual("async from stream");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_ZipFrom_IncludeBaseDirectory(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var sourceDir = Path.Combine(root, "mydir");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "file.txt"), "content");

		var zipPath = Path.Combine(root, "with_base.zip");
		fs.ZipFrom(sourceDir, zipPath, includeBaseDirectory: true);

		using var zipEntries = fs.Unzip(zipPath);
		var list = zipEntries.ToList();
		list.Count.AssertEqual(1);
		list[0].name.Contains("mydir").AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_Unzip_WithFilter(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var zipPath = Path.Combine(root, "filter_test.zip");
		var entries = new List<(string name, Stream body)>
		{
			("include.txt", new MemoryStream("include"u8.ToArray())),
			("exclude.dat", new MemoryStream("exclude"u8.ToArray())),
			("also_include.txt", new MemoryStream("also"u8.ToArray()))
		};
		fs.Zip(zipPath, entries);

		using var filtered = fs.Unzip(zipPath, name => name.EndsWith(".txt"));
		var list = filtered.ToList();

		list.Count.AssertEqual(2);
		list.All(e => e.name.EndsWith(".txt")).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_ReadEntries_ReturnsStreams(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");

		var subDir = Path.Combine(sourceDir, "sub");
		fs.CreateDirectory(subDir);
		fs.WriteAllText(Path.Combine(subDir, "file2.txt"), "content2");

		var entries = fs.ReadEntries(sourceDir).ToList();

		entries.Count.AssertEqual(2);
		entries.Any(e => e.name == "file1.txt").AssertTrue();
		entries.Any(e => e.name == "sub/file2.txt").AssertTrue();

		// Read content from streams
		foreach (var (name, body) in entries)
		{
			using var reader = new StreamReader(body);
			var content = reader.ReadToEnd();
			content.IsEmpty().AssertFalse();
			body.Dispose();
		}
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_ReadEntries_WithPattern(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "txt");
		fs.WriteAllText(Path.Combine(sourceDir, "file2.dat"), "dat");
		fs.WriteAllText(Path.Combine(sourceDir, "file3.txt"), "txt2");

		var entries = fs.ReadEntries(sourceDir, "*.txt").ToList();

		entries.Count.AssertEqual(2);
		entries.All(e => e.name.EndsWith(".txt")).AssertTrue();

		foreach (var (_, body) in entries)
			body.Dispose();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_WriteEntries_CreatesFiles(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var destDir = Path.Combine(root, "dest");

		var entries = new List<(string name, Stream body)>
		{
			("file1.txt", new MemoryStream("content1"u8.ToArray())),
			("sub/file2.txt", new MemoryStream("content2"u8.ToArray()))
		};

		fs.WriteEntries(destDir, entries);

		fs.FileExists(Path.Combine(destDir, "file1.txt")).AssertTrue();
		fs.ReadAllText(Path.Combine(destDir, "file1.txt")).AssertEqual("content1");

		fs.FileExists(Path.Combine(destDir, "sub", "file2.txt")).AssertTrue();
		fs.ReadAllText(Path.Combine(destDir, "sub", "file2.txt")).AssertEqual("content2");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_WriteEntries_OverwriteOption(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var destDir = Path.Combine(root, "dest");
		fs.CreateDirectory(destDir);
		fs.WriteAllText(Path.Combine(destDir, "file.txt"), "old");

		var entries = new List<(string name, Stream body)>
		{
			("file.txt", new MemoryStream("new"u8.ToArray()))
		};

		// overwrite=true
		fs.WriteEntries(destDir, entries, overwrite: true);
		fs.ReadAllText(Path.Combine(destDir, "file.txt")).AssertEqual("new");

		// Reset
		fs.WriteAllText(Path.Combine(destDir, "file.txt"), "old");

		entries = new List<(string name, Stream body)>
		{
			("file.txt", new MemoryStream("new2"u8.ToArray()))
		};

		// overwrite=false
		fs.WriteEntries(destDir, entries, overwrite: false);
		fs.ReadAllText(Path.Combine(destDir, "file.txt")).AssertEqual("old");
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task FileSystem_WriteEntriesAsync_CreatesFiles(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var destDir = Path.Combine(root, "dest");

		var entries = new List<(string name, Stream body)>
		{
			("async1.txt", new MemoryStream("async content 1"u8.ToArray())),
			("dir/async2.txt", new MemoryStream("async content 2"u8.ToArray()))
		};

		await fs.WriteEntriesAsync(destDir, entries, cancellationToken: CancellationToken);

		fs.FileExists(Path.Combine(destDir, "async1.txt")).AssertTrue();
		fs.FileExists(Path.Combine(destDir, "dir", "async2.txt")).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void FileSystem_ReadEntries_WriteEntries_RoundTrip(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// Create source
		var sourceDir = Path.Combine(root, "source");
		fs.CreateDirectory(sourceDir);
		fs.WriteAllText(Path.Combine(sourceDir, "a.txt"), "aaa");
		fs.WriteAllText(Path.Combine(sourceDir, "b.txt"), "bbb");

		var subDir = Path.Combine(sourceDir, "sub");
		fs.CreateDirectory(subDir);
		fs.WriteAllText(Path.Combine(subDir, "c.txt"), "ccc");

		// Read entries and write to dest
		var destDir = Path.Combine(root, "dest");
		var entries = fs.ReadEntries(sourceDir).ToList();
		fs.WriteEntries(destDir, entries);

		// Dispose streams
		foreach (var (_, body) in entries)
			body.Dispose();

		// Verify
		fs.ReadAllText(Path.Combine(destDir, "a.txt")).AssertEqual("aaa");
		fs.ReadAllText(Path.Combine(destDir, "b.txt")).AssertEqual("bbb");
		fs.ReadAllText(Path.Combine(destDir, "sub", "c.txt")).AssertEqual("ccc");
	}

	#endregion
}