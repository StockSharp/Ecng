namespace Ecng.Backup.Mega.Native;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

internal abstract class MegaAesCtrStream : Stream
{
	protected enum Mode
	{
		Crypt,
		Decrypt,
	}

	protected readonly byte[] FileKey;
	protected readonly byte[] Iv;
	protected readonly long StreamLength;

	protected readonly byte[] MetaMac = new byte[8];
	protected long _position;

	private readonly Stream _stream;
	private readonly Mode _mode;
	private readonly HashSet<long> _chunksPositionsCache;
	private readonly byte[] _counter = new byte[8];
	private readonly ICryptoTransform _encryptor;

	private long _currentCounter;
	private byte[] _currentChunkMac = new byte[16];
	private byte[] _fileMac = new byte[16];

	public long[] ChunksPositions { get; }

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => StreamLength;

	public override long Position
	{
		get => _position;
		set
		{
			if (_position != value)
				throw new NotSupportedException("Seek is not supported.");
		}
	}

	protected MegaAesCtrStream(Stream stream, long streamLength, Mode mode, byte[] fileKey, byte[] iv)
	{
		if (fileKey is null || fileKey.Length != 16)
			throw new ArgumentException("Invalid fileKey.", nameof(fileKey));

		if (iv is null || iv.Length != 8)
			throw new ArgumentException("Invalid iv.", nameof(iv));

		_stream = stream ?? throw new ArgumentNullException(nameof(stream));
		StreamLength = streamLength;
		_mode = mode;
		FileKey = fileKey;
		Iv = iv;

		ChunksPositions = GetChunksPositions(streamLength).ToArray();
		_chunksPositionsCache = new HashSet<long>(ChunksPositions);
		_encryptor = Crypto.CreateAesEncryptor(FileKey);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		_encryptor.Dispose();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (_position == StreamLength)
			return 0;

		if (_position + count < StreamLength && count < 16)
			throw new NotSupportedException("Minimal read operation must be >= 16 bytes (except last read).");

		count = (_position + count < StreamLength) ? (count - count % 16) : count;

		for (long pos = _position; pos < Math.Min(_position + count, StreamLength); pos += 16)
		{
			if (_chunksPositionsCache.Contains(pos))
			{
				if (pos != 0)
					ComputeChunk();

				for (var i = 0; i < 8; i++)
				{
					_currentChunkMac[i] = Iv[i];
					_currentChunkMac[i + 8] = Iv[i];
				}
			}

			IncrementCounter();

			var block = new byte[16];
			var outBlock = new byte[16];

			var read = _stream.Read(block, 0, block.Length);
			if (read != block.Length)
				read += _stream.Read(block, read, block.Length - read);

			var ivBlock = new byte[16];
			Array.Copy(Iv, ivBlock, 8);
			Array.Copy(_counter, 0, ivBlock, 8, 8);

			var keystream = Crypto.EncryptAes(ivBlock, _encryptor);

			for (var i = 0; i < read; i++)
			{
				outBlock[i] = (byte)(keystream[i] ^ block[i]);
				_currentChunkMac[i] ^= (_mode == Mode.Crypt) ? block[i] : outBlock[i];
			}

			Array.Copy(outBlock, 0, buffer, (int)(offset + pos - _position), (int)Math.Min(outBlock.Length, StreamLength - pos));
			_currentChunkMac = Crypto.EncryptAes(_currentChunkMac, _encryptor);
		}

		var advanced = Math.Min(count, StreamLength - _position);
		_position += advanced;

		if (_position == StreamLength)
		{
			ComputeChunk();

			for (var k = 0; k < 4; k++)
			{
				MetaMac[k] = (byte)(_fileMac[k] ^ _fileMac[k + 4]);
				MetaMac[k + 4] = (byte)(_fileMac[k + 8] ^ _fileMac[k + 12]);
			}

			OnStreamRead();
		}

		return (int)advanced;
	}

	public override void Flush() => throw new NotSupportedException();
	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected virtual void OnStreamRead()
	{
	}

	private void IncrementCounter()
	{
		if ((_currentCounter & 0xFF) != 255 && (_currentCounter & 0xFF) != 0)
		{
			_counter[7]++;
		}
		else
		{
			var bytes = BitConverter.GetBytes(_currentCounter);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			Array.Copy(bytes, _counter, 8);
		}

		_currentCounter++;
	}

	private void ComputeChunk()
	{
		for (var i = 0; i < 16; i++)
			_fileMac[i] ^= _currentChunkMac[i];

		_fileMac = Crypto.EncryptAes(_fileMac, _encryptor);
	}

	private static IEnumerable<long> GetChunksPositions(long size)
	{
		yield return 0;

		long start = 0;

		for (var idx = 1; idx <= 8 && start < size - idx * 131072; idx++)
		{
			start += idx * 131072;
			yield return start;
		}

		while (start + 1048576 < size)
		{
			start += 1048576;
			yield return start;
		}
	}
}

internal sealed class MegaAesCtrStreamCrypter : MegaAesCtrStream
{
	public byte[] FileKeyBytes => FileKey;
	public byte[] IvBytes => Iv;

	public byte[] ComputedMetaMac
	{
		get
		{
			if (_position != StreamLength)
				throw new NotSupportedException("Stream must be fully read to obtain MetaMac.");

			return MetaMac;
		}
	}

	public MegaAesCtrStreamCrypter(Stream stream)
		: base(stream, stream.Length, Mode.Crypt, Crypto.CreateAesKey(), Crypto.CreateAesKey().AsSpan(0, 8).ToArray())
	{
	}
}

internal sealed class MegaAesCtrStreamDecrypter : MegaAesCtrStream
{
	private readonly byte[] _expectedMetaMac;

	public MegaAesCtrStreamDecrypter(Stream stream, long streamLength, byte[] fileKey, byte[] iv, byte[] expectedMetaMac)
		: base(stream, streamLength, Mode.Decrypt, fileKey, iv)
	{
		if (expectedMetaMac is null || expectedMetaMac.Length != 8)
			throw new ArgumentException("Invalid expectedMetaMac.", nameof(expectedMetaMac));

		_expectedMetaMac = expectedMetaMac;
	}

	protected override void OnStreamRead()
	{
		if (!_expectedMetaMac.SequenceEqual(MetaMac))
			throw new InvalidOperationException("Checksum is invalid. Downloaded data are corrupted.");
	}
}

internal sealed class CancellableStream : Stream
{
	private readonly Stream _inner;
	private readonly CancellationToken _cancellationToken;

	public CancellableStream(Stream inner, CancellationToken cancellationToken)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_cancellationToken = cancellationToken;
	}

	public override bool CanRead => _inner.CanRead;
	public override bool CanSeek => _inner.CanSeek;
	public override bool CanWrite => _inner.CanWrite;
	public override long Length => _inner.Length;

	public override long Position
	{
		get => _inner.Position;
		set => _inner.Position = value;
	}

	public override void Flush()
	{
		_cancellationToken.ThrowIfCancellationRequested();
		_inner.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		_cancellationToken.ThrowIfCancellationRequested();
		return _inner.Read(buffer, offset, count);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		_cancellationToken.ThrowIfCancellationRequested();
		return _inner.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		_cancellationToken.ThrowIfCancellationRequested();
		_inner.SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_cancellationToken.ThrowIfCancellationRequested();
		_inner.Write(buffer, offset, count);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_inner.Dispose();

		base.Dispose(disposing);
	}
}
