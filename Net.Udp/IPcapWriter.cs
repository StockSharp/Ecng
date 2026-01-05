namespace Ecng.Net.Udp;

using System;
using System.IO;

using Ecng.IO;

/// <summary>
/// Interface for writing packets in PCAP format.
/// </summary>
public interface IPcapWriter : IDisposable
{
	/// <summary>
	/// Gets the current size of written data in bytes.
	/// </summary>
	long CurrentSize { get; }

	/// <summary>
	/// Writes a raw capture to the PCAP file.
	/// </summary>
	/// <param name="timestamp">The packet timestamp.</param>
	/// <param name="data">The packet data.</param>
	void Write(DateTime timestamp, byte[] data);
}

/// <summary>
/// Factory for creating PCAP writers.
/// </summary>
public interface IPcapWriterFactory
{
	/// <summary>
	/// Creates a new PCAP writer for the specified path.
	/// </summary>
	/// <param name="path">The file path.</param>
	/// <returns>The PCAP writer.</returns>
	IPcapWriter Create(string path);
}

/// <summary>
/// PCAP writer that writes to IFileSystem using stream.
/// Implements PCAP file format directly.
/// </summary>
public class PcapStreamWriter : IPcapWriter
{
	// PCAP magic number (microsecond resolution)
	private const uint MagicNumber = 0xa1b2c3d4;
	private const ushort VersionMajor = 2;
	private const ushort VersionMinor = 4;
	private const uint MaxPacketLength = 65535;
	private const uint LinkTypeEthernet = 1;

	private readonly Stream _stream;
	private long _currentSize;
	private bool _isDisposed;

	/// <summary>
	/// Initializes a new instance using IFileSystem.
	/// </summary>
	/// <param name="fileSystem">The file system.</param>
	/// <param name="path">The file path.</param>
	public PcapStreamWriter(IFileSystem fileSystem, string path)
	{
		if (fileSystem == null)
			throw new ArgumentNullException(nameof(fileSystem));
		if (string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir) && !fileSystem.DirectoryExists(dir))
			fileSystem.CreateDirectory(dir);

		_stream = fileSystem.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
		WriteGlobalHeader();
	}

	/// <summary>
	/// Initializes a new instance with an existing stream.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	public PcapStreamWriter(Stream stream)
	{
		_stream = stream ?? throw new ArgumentNullException(nameof(stream));
		WriteGlobalHeader();
	}

	/// <inheritdoc />
	public long CurrentSize => _currentSize;

	private void WriteGlobalHeader()
	{
		// PCAP global header (24 bytes)
		var header = new byte[24];
		var offset = 0;

		// Magic number
		WriteUInt32(header, ref offset, MagicNumber);
		// Version major
		WriteUInt16(header, ref offset, VersionMajor);
		// Version minor
		WriteUInt16(header, ref offset, VersionMinor);
		// Timezone offset (GMT)
		WriteInt32(header, ref offset, 0);
		// Timestamp accuracy
		WriteUInt32(header, ref offset, 0);
		// Max packet length
		WriteUInt32(header, ref offset, MaxPacketLength);
		// Link-layer type (Ethernet)
		WriteUInt32(header, ref offset, LinkTypeEthernet);

		_stream.Write(header, 0, header.Length);
		_currentSize = header.Length;
	}

	/// <inheritdoc />
	public void Write(DateTime timestamp, byte[] data)
	{
		if (_isDisposed)
			throw new ObjectDisposedException(nameof(PcapStreamWriter));
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		// PCAP packet header (16 bytes)
		var packetHeader = new byte[16];
		var offset = 0;

		// Convert to Unix timestamp
		var unixTime = (uint)((DateTimeOffset)timestamp).ToUnixTimeSeconds();
		var microseconds = (uint)(timestamp.Ticks % TimeSpan.TicksPerSecond / (TimeSpan.TicksPerSecond / 1_000_000));

		// Timestamp seconds
		WriteUInt32(packetHeader, ref offset, unixTime);
		// Timestamp microseconds
		WriteUInt32(packetHeader, ref offset, microseconds);
		// Captured length
		WriteUInt32(packetHeader, ref offset, (uint)data.Length);
		// Original length
		WriteUInt32(packetHeader, ref offset, (uint)data.Length);

		_stream.Write(packetHeader, 0, packetHeader.Length);
		_stream.Write(data, 0, data.Length);

		_currentSize += packetHeader.Length + data.Length;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;
		_stream?.Flush();
		_stream?.Dispose();
	}

	private static void WriteUInt32(byte[] buffer, ref int offset, uint value)
	{
		buffer[offset++] = (byte)(value & 0xFF);
		buffer[offset++] = (byte)((value >> 8) & 0xFF);
		buffer[offset++] = (byte)((value >> 16) & 0xFF);
		buffer[offset++] = (byte)((value >> 24) & 0xFF);
	}

	private static void WriteUInt16(byte[] buffer, ref int offset, ushort value)
	{
		buffer[offset++] = (byte)(value & 0xFF);
		buffer[offset++] = (byte)((value >> 8) & 0xFF);
	}

	private static void WriteInt32(byte[] buffer, ref int offset, int value)
	{
		WriteUInt32(buffer, ref offset, (uint)value);
	}
}

/// <summary>
/// Factory that creates PcapStreamWriter instances.
/// </summary>
public class PcapStreamWriterFactory : IPcapWriterFactory
{
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="fileSystem">The file system to use.</param>
	public PcapStreamWriterFactory(IFileSystem fileSystem)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
	}

	/// <inheritdoc />
	public IPcapWriter Create(string path)
	{
		return new PcapStreamWriter(_fileSystem, path);
	}
}
