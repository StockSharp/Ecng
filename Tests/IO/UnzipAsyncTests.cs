namespace Ecng.Tests.IO;

using System.IO.Compression;

using Ecng.IO;
using Ecng.IO.Compression;

[TestClass]
public class UnzipAsyncTests : BaseTestClass
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ReadsNonSeekableInputAsynchronously(bool leaveOpen)
	{
		using var input = new ArchiveInput(CreateArchive(("a.txt", "hello"), ("dir/b.txt", "world")));
		var actual = new Dictionary<string, string>();

		await foreach (var (name, body) in input.UnzipAsync(leaveOpen, cancellationToken: CancellationToken))
			actual.Add(name, await ReadText(body));

		AreEqual(2, actual.Count);
		AreEqual("hello", actual["a.txt"]);
		AreEqual("world", actual["dir/b.txt"]);
		IsGreater(input.AsyncReadCount, 0);
		AreEqual(!leaveOpen, input.IsDisposed);
		AreEqual(!leaveOpen, input.IsDisposedAsynchronously);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task FiltersByEntryNameAndReturnsFullPaths()
	{
		using var input = new MemoryStream(CreateArchive(("first/data.csv", "one"), ("notes.txt", "skip"), ("second/data.csv", "two")));
		var actual = new Dictionary<string, string>();

		await foreach (var (name, body) in input.UnzipAsync(filter: name => name == "data.csv", cancellationToken: CancellationToken))
			actual.Add(name, await ReadText(body));

		AreEqual(2, actual.Count);
		AreEqual("one", actual["first/data.csv"]);
		AreEqual("two", actual["second/data.csv"]);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ByteArrayOverloadReadsEveryEntry()
	{
		var input = CreateArchive(("a.txt", "hello"), ("dir/b.txt", "world"));
		var actual = new List<string>();

		await foreach (var (name, body) in input.UnzipAsync(cancellationToken: CancellationToken))
			actual.Add($"{name}:{await ReadText(body)}");

		AreEqual(new[] { "a.txt:hello", "dir/b.txt:world" }, actual.ToArray());
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task EmptyArchiveHonorsLeaveOpen(bool leaveOpen)
	{
		using var input = new ArchiveInput(CreateArchive());
		var count = 0;

		await foreach (var entry in input.UnzipAsync(leaveOpen, cancellationToken: CancellationToken))
			count++;

		AreEqual(0, count);
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CancelsDuringNonSeekableRead(bool leaveOpen, bool isEnumeratorToken)
	{
		using var cancellation = CancellationToken.CreateChildToken().cts;
		using var input = new ArchiveInput(null) { OnReadAsync = cancellation.Cancel };
		var entries = input.UnzipAsync(leaveOpen, cancellationToken: isEnumeratorToken ? CancellationToken : cancellation.Token);

		await ThrowsAsync<OperationCanceledException>(async () =>
		{
			await using var iterator = entries.GetAsyncEnumerator(isEnumeratorToken ? cancellation.Token : CancellationToken);
			await iterator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), CancellationToken);
		});

		IsGreater(input.AsyncReadCount, 0);
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CanceledEnumerationDoesNotReadTheInput(bool leaveOpen)
	{
		using var cancellation = CancellationToken.CreateChildToken().cts;
		cancellation.Cancel();
		using var input = new ArchiveInput(CreateArchive(("a.txt", "hello")));

		await ThrowsAsync<OperationCanceledException>(async () =>
		{
			await using var iterator = input.UnzipAsync(leaveOpen, cancellationToken: cancellation.Token).GetAsyncEnumerator(CancellationToken);
			await iterator.MoveNextAsync();
		});

		AreEqual(0, input.AsyncReadCount);
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CancellationBetweenEntriesDisposesTheCurrentBody(bool leaveOpen)
	{
		using var cancellation = CancellationToken.CreateChildToken().cts;
		using var input = new ArchiveInput(CreateArchive(("a.txt", "hello"), ("b.txt", "world")));
		await using var iterator = input.UnzipAsync(leaveOpen, cancellationToken: cancellation.Token).GetAsyncEnumerator(CancellationToken);
		IsTrue(await iterator.MoveNextAsync());
		var body = iterator.Current.body;
		cancellation.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () => await iterator.MoveNextAsync());

		IsFalse(body.CanRead);
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MalformedArchivesHonorLeaveOpen(bool leaveOpen)
	{
		using var input = new ArchiveInput("not a zip".UTF8());

		await ThrowsAsync<InvalidDataException>(async () =>
		{
			await using var iterator = input.UnzipAsync(leaveOpen, cancellationToken: CancellationToken).GetAsyncEnumerator();
			await iterator.MoveNextAsync();
		});

		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ReadFailuresHonorLeaveOpen(bool leaveOpen)
	{
		var expected = new IOException("Connection reset while downloading archive.");
		using var input = new ArchiveInput(null) { OnReadAsync = () => throw expected };

		var error = await ThrowsAsync<IOException>(async () =>
		{
			await using var iterator = input.UnzipAsync(leaveOpen, cancellationToken: CancellationToken).GetAsyncEnumerator();
			await iterator.MoveNextAsync();
		});

		IsTrue(ReferenceEquals(expected, error));
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task FilterFailuresHonorLeaveOpen(bool leaveOpen)
	{
		var expected = new InvalidOperationException("Filter failed.");
		using var input = new ArchiveInput(CreateArchive(("a.txt", "hello")));

		var error = await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await using var iterator = input.UnzipAsync(leaveOpen, filter: _ => throw expected, cancellationToken: CancellationToken).GetAsyncEnumerator();
			await iterator.MoveNextAsync();
		});

		IsTrue(ReferenceEquals(expected, error));
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ConsumerFailuresHonorLeaveOpen(bool leaveOpen)
	{
		var expected = new InvalidOperationException("Consumer failed.");
		using var input = new ArchiveInput(CreateArchive(("a.txt", "hello")));
		Stream current = null;

		var error = await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await foreach (var (_, body) in input.UnzipAsync(leaveOpen, cancellationToken: CancellationToken))
			{
				current = body;
				throw expected;
			}
		});

		IsTrue(ReferenceEquals(expected, error));
		IsNotNull(current);
		IsFalse(current.CanRead);
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MovingAndStoppingEnumerationClosesEntryStreams(bool leaveOpen)
	{
		using var input = new ArchiveInput(CreateArchive(("a.txt", "hello"), ("b.txt", "world")));
		Stream current;

		await using (var iterator = input.UnzipAsync(leaveOpen, cancellationToken: CancellationToken).GetAsyncEnumerator())
		{
			IsTrue(await iterator.MoveNextAsync());
			var previous = iterator.Current.body;
			IsTrue(await iterator.MoveNextAsync());
			IsFalse(previous.CanRead);
			current = iterator.Current.body;
			IsTrue(current.CanRead);
		}

		IsFalse(current.CanRead);
		AreEqual(!leaveOpen, input.IsDisposed);
	}

	[TestMethod]
	public void NullInputsAreRejected()
	{
		Throws<ArgumentNullException>(() => ((Stream)null).UnzipAsync());
		Throws<ArgumentNullException>(() => ((byte[])null).UnzipAsync());
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task UnzipToAsyncReadsNonSeekableInputAndPreservesExtractionRules(bool overwrite)
	{
		var (fs, root) = Config.CreateFs(nameof(MemoryFileSystem));
		var destination = Path.Combine(root, "destination");
		fs.CreateDirectory(destination);
		fs.WriteAllText(Path.Combine(destination, "existing.txt"), "old");
		using var input = new ArchiveInput(CreateArchive(("existing.txt", "new"), ("dir/data.txt", "nested"), ("empty/", ""), ("../outside.txt", "unsafe")));

		await fs.UnzipToAsync(input, destination, overwrite, CancellationToken);

		AreEqual(overwrite ? "new" : "old", fs.ReadAllText(Path.Combine(destination, "existing.txt")));
		AreEqual("nested", fs.ReadAllText(Path.Combine(destination, "dir", "data.txt")));
		IsTrue(fs.DirectoryExists(Path.Combine(destination, "empty")));
		IsFalse(fs.FileExists(Path.Combine(root, "outside.txt")));
		IsFalse(input.IsDisposed);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task UnzipToAsyncCancelsWhileDownloadingTheArchive()
	{
		var (fs, root) = Config.CreateFs(nameof(MemoryFileSystem));
		using var cancellation = CancellationToken.CreateChildToken().cts;
		using var input = new ArchiveInput(null) { OnReadAsync = cancellation.Cancel };

		await ThrowsAsync<OperationCanceledException>(() => fs.UnzipToAsync(input, root, cancellationToken: cancellation.Token)
			.WaitAsync(TimeSpan.FromSeconds(2), CancellationToken));

		IsFalse(input.IsDisposed);
	}

	private async Task<string> ReadText(Stream body)
	{
		using var output = new MemoryStream();
		await body.CopyToAsync(output, CancellationToken);
		return output.ToArray().UTF8();
	}

	private static byte[] CreateArchive(params (string name, string content)[] entries)
	{
		using var output = new MemoryStream();
		using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
		{
			foreach (var (name, content) in entries)
			{
				using var body = archive.CreateEntry(name).Open();
				body.Write(content.UTF8());
			}
		}

		return output.ToArray();
	}

	private sealed class ArchiveInput(byte[] data) : Stream
	{
		private readonly MemoryStream _source = data is null ? null : new(data, false);

		public Action OnReadAsync { get; init; }

		public int AsyncReadCount { get; private set; }

		public bool IsDisposed { get; private set; }

		public bool IsDisposedAsynchronously { get; private set; }

		public override bool CanRead => !IsDisposed;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override int Read(byte[] buffer, int offset, int count)
			=> throw new InvalidOperationException("Synchronous archive reads are forbidden.");

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			AsyncReadCount++;
			OnReadAsync?.Invoke();

			if (_source is not null)
				return await _source.ReadAsync(buffer[..Math.Min(buffer.Length, 17)], cancellationToken);

			await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			return 0;
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

		public override void Flush() => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override ValueTask DisposeAsync()
		{
			IsDisposedAsynchronously = true;
			Dispose();
			return default;
		}

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			if (disposing)
				_source?.Dispose();
			base.Dispose(disposing);
		}
	}
}
