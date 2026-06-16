namespace Ecng.Tests.Backup;

using System.IO;

using Ecng.Backup.Mega.Native;

/// <summary>
/// Deterministic unit tests for Backup.Mega native crypto/stream internals. Unlike
/// <see cref="MegaNativeClientTests"/> these need no live MEGA account, so they actually run in CI
/// and reproduce the audit findings. They reach the internals directly via
/// [InternalsVisibleTo("Ecng.Tests")].
/// </summary>
[TestClass]
public class MegaNativeCryptoTests : BaseTestClass
{
	// A read-only stream that never returns more than one byte per Read, emulating the partial
	// reads network streams are allowed to perform.
	private sealed class OneByteAtATimeStream(byte[] data) : Stream
	{
		private readonly byte[] _data = data ?? throw new ArgumentNullException(nameof(data));
		private int _pos;

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => _data.Length;
		public override long Position { get => _pos; set => throw new NotSupportedException(); }

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count <= 0 || _pos >= _data.Length)
				return 0;

			buffer[offset] = _data[_pos++];
			return 1;
		}

		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	private static byte[] ReadFully(Stream stream, int length)
	{
		var buffer = new byte[length];
		var offset = 0;

		while (offset < length)
		{
			var read = stream.Read(buffer, offset, length - offset);

			if (read == 0)
				break;

			offset += read;
		}

		return buffer;
	}

	/// <summary>
	/// Finding #1: MegaAesCtrStream.Read fills each 16-byte block with at most two Stream.Read
	/// calls instead of a full drain loop (Backup.Mega\Native\Streams.cs:101), so a source that
	/// returns fewer bytes than requested leaves the block partially filled, corrupts the
	/// keystream/MAC and yields wrong data (or a false "Checksum is invalid"). Decrypting through a
	/// one-byte-at-a-time source must reproduce the exact plaintext.
	/// </summary>
	[TestMethod]
	public void AesCtr_ShortReadingSource_DecryptsToOriginal()
	{
		var plain = new byte[48];
		for (var i = 0; i < plain.Length; i++)
			plain[i] = (byte)(i * 7 + 1);

		byte[] cipher, fileKey, iv, metaMac;

		// Encrypt against a normal (full-read) MemoryStream to obtain the ciphertext + key material.
		using (var encrypter = new MegaAesCtrStreamCrypter(new MemoryStream(plain)))
		{
			cipher = ReadFully(encrypter, plain.Length);
			fileKey = encrypter.FileKeyBytes;
			iv = encrypter.IvBytes;
			metaMac = encrypter.ComputedMetaMac;
		}

		// Decrypt where the underlying stream drips one byte per Read (network-like partial reads).
		using var decrypter = new MegaAesCtrStreamDecrypter(new OneByteAtATimeStream(cipher), cipher.Length, fileKey, iv, metaMac);
		var roundTripped = ReadFully(decrypter, plain.Length);

		roundTripped.SequenceEqual(plain).AssertTrue("Decrypting a short-reading source must reproduce the plaintext.");
	}

	/// <summary>
	/// Finding #4: Crypto.FromEpochSeconds returns epoch.AddSeconds(seconds).ToLocalTime()
	/// (Backup.Mega\Native\Crypto.cs:336), so timestamps carry machine-local DateTime values
	/// (Kind=Local), breaking the UTC convention and symmetry with ToEpochSeconds. The result must
	/// be UTC.
	/// </summary>
	[TestMethod]
	public void FromEpochSeconds_ReturnsUtc()
	{
		const long seconds = 1_700_000_000L;

		var dt = seconds.FromEpochSeconds();

		AreEqual(DateTimeKind.Utc, dt.Kind);
		AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds), dt);
	}

	/// <summary>
	/// Finding #8: Crypto.SerializeToBytes shifts a long via arithmetic ">>= 8"
	/// (Backup.Mega\Native\Crypto.cs:347); a negative value converges to -1 and never reaches 0, so
	/// the loop overruns the 9-byte buffer (IndexOutOfRangeException). A pre-1970 modification date
	/// (negative epoch seconds) must serialize and round-trip back to the same value.
	/// </summary>
	[TestMethod]
	public void Serialize_NegativeEpoch_RoundTrips()
	{
		var value = new DateTime(1960, 6, 15, 12, 0, 0, DateTimeKind.Utc).ToEpochSeconds();
		(value < 0).AssertTrue("Pre-1970 date must yield a negative epoch second value");

		var bytes = value.SerializeToBytes();
		var back = bytes.DeserializeToLong(0, bytes.Length);

		AreEqual(value, back);
	}

	/// <summary>
	/// Finding #9: MegaAttributes.HydrateAfterDeserialize unconditionally calls
	/// DeserializeToLong(16, ...) (Backup.Mega\Native\Crypto.cs:407), so a fingerprint shorter than
	/// 17 bytes (a corrupt or third-party 'c' attribute) throws and aborts parsing of the whole node
	/// tree. A short fingerprint must degrade gracefully, leaving ModificationDate null.
	/// </summary>
	[TestMethod]
	public void Hydrate_ShortFingerprint_LeavesModificationDateNull()
	{
		var attrs = new MegaAttributes { SerializedFingerprint = new byte[16].ToBase64Url() };

		attrs.HydrateAfterDeserialize();

		IsNull(attrs.ModificationDate);
	}
}
