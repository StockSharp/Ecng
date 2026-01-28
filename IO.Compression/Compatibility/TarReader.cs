#if !NET7_0_OR_GREATER
namespace System.Formats.Tar;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

using SharpCompress.Common;
using SharpCompress.Readers;

using SharpTarReader = SharpCompress.Readers.Tar.TarReader;

/// <summary>
/// Polyfill for System.Formats.Tar.TarEntryType for .NET 6.
/// </summary>
public enum TarEntryType : byte
{
	/// <summary>Regular file.</summary>
	RegularFile = (byte)'0',
	/// <summary>Hard link.</summary>
	HardLink = (byte)'1',
	/// <summary>Symbolic link.</summary>
	SymbolicLink = (byte)'2',
	/// <summary>Character device.</summary>
	CharacterDevice = (byte)'3',
	/// <summary>Block device.</summary>
	BlockDevice = (byte)'4',
	/// <summary>Directory.</summary>
	Directory = (byte)'5',
	/// <summary>FIFO.</summary>
	Fifo = (byte)'6',
	/// <summary>Contiguous file.</summary>
	ContiguousFile = (byte)'7',
	/// <summary>Global extended header.</summary>
	GlobalExtendedAttributes = (byte)'g',
	/// <summary>Extended header.</summary>
	ExtendedAttributes = (byte)'x',
}

/// <summary>
/// Polyfill for System.Formats.Tar.TarEntry for .NET 6.
/// </summary>
public sealed class TarEntry
{
	/// <summary>
	/// Gets the name of the entry.
	/// </summary>
	public string Name { get; internal set; }

	/// <summary>
	/// Gets the entry type.
	/// </summary>
	public TarEntryType EntryType { get; internal set; }

	/// <summary>
	/// Gets the data stream for the entry content.
	/// </summary>
	public Stream DataStream { get; internal set; }

	/// <summary>
	/// Gets the size of the entry data in bytes.
	/// </summary>
	public long Length { get; internal set; }
}

/// <summary>
/// Polyfill for System.Formats.Tar.TarReader for .NET 6.
/// Uses SharpCompress TarReader (streaming) internally.
/// </summary>
public sealed class TarReader : IDisposable
{
	private readonly IReader _reader;
	private readonly bool _leaveOpen;
	private readonly Stream _stream;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="TarReader"/> class.
	/// </summary>
	/// <param name="archiveStream">The stream containing the TAR archive.</param>
	/// <param name="leaveOpen">Whether to leave the stream open after disposing.</param>
	public TarReader(Stream archiveStream, bool leaveOpen = false)
	{
		_stream = archiveStream ?? throw new ArgumentNullException(nameof(archiveStream));
		_leaveOpen = leaveOpen;
		_reader = SharpTarReader.Open(archiveStream, new ReaderOptions { LeaveStreamOpen = true });
	}

	/// <summary>
	/// Gets the next entry from the TAR archive.
	/// </summary>
	/// <param name="copyData">Whether to copy entry data to a separate stream.</param>
	/// <returns>The next entry, or null if no more entries.</returns>
	public TarEntry GetNextEntry(bool copyData = true)
	{
		ThrowIfDisposed();

		if (!_reader.MoveToNextEntry())
			return null;

		var sharpEntry = _reader.Entry;
		return ConvertEntry(_reader, sharpEntry, copyData);
	}

	/// <summary>
	/// Gets the next entry from the TAR archive asynchronously.
	/// </summary>
	/// <param name="copyData">Whether to copy entry data to a separate stream.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The next entry, or null if no more entries.</returns>
	public Task<TarEntry> GetNextEntryAsync(bool copyData = true, CancellationToken cancellationToken = default)
	{
		// SharpCompress doesn't have async API, so we just wrap sync call
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(GetNextEntry(copyData));
	}

	private static TarEntry ConvertEntry(IReader reader, IEntry sharpEntry, bool copyData)
	{
		var entryType = sharpEntry.IsDirectory
			? TarEntryType.Directory
			: TarEntryType.RegularFile;

		Stream dataStream = null;
		if (!sharpEntry.IsDirectory && sharpEntry.Size > 0)
		{
			if (copyData)
			{
				// Copy to MemoryStream for safe access
				dataStream = new MemoryStream();
				using (var entryStream = reader.OpenEntryStream())
				{
					entryStream.CopyTo(dataStream);
				}
				dataStream.Position = 0;
			}
			else
			{
				dataStream = reader.OpenEntryStream();
			}
		}

		return new TarEntry
		{
			Name = sharpEntry.Key,
			EntryType = entryType,
			Length = sharpEntry.Size,
			DataStream = dataStream
		};
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(TarReader));
	}

	/// <summary>
	/// Disposes the reader.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_reader.Dispose();

		if (!_leaveOpen)
			_stream.Dispose();
	}
}
#endif
