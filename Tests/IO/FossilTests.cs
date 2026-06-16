namespace Ecng.Tests.IO;

using Ecng.IO.Fossil;

using Ecng.IO.Compression;

[TestClass]
public class FossilTests : BaseTestClass
{
	[TestMethod]
	public async Task Diff()
	{
		var token = CancellationToken;

		var bytes1 = RandomGen.GetBytes(FileSizes.MB);
		var bytes2 = RandomGen.GetBytes(FileSizes.MB);

		var delta = await Delta.Create(bytes1, bytes2, token);

		(await Delta.Apply(bytes1, delta, token)).AssertEqual(bytes2);
	}

	private async Task AssertRoundTrip(byte[] origin, byte[] target)
	{
		var delta = await Delta.Create(origin, target, CancellationToken);
		var restored = await Delta.Apply(origin, delta, CancellationToken);
		restored.AssertEqual(target);
	}

	// Sizes around NHASH (16) and the small-source fast-path, plus larger blocks.
	[DataRow(0, 0)]
	[DataRow(0, 1)]
	[DataRow(1, 0)]
	[DataRow(1, 1)]
	[DataRow(15, 15)]
	[DataRow(16, 16)]
	[DataRow(17, 17)]
	[DataRow(16, 0)]
	[DataRow(0, 16)]
	[DataRow(FileSizes.KB, FileSizes.KB)]
	[DataRow(FileSizes.KB, FileSizes.KB + 1)]
	[DataRow(FileSizes.KB + 1, FileSizes.KB)]
	[DataRow(64 * FileSizes.KB, 64 * FileSizes.KB)]
	[TestMethod]
	public async Task RoundTrip_RandomPair(int originSize, int targetSize)
	{
		var origin = RandomGen.GetBytes(originSize);
		var target = RandomGen.GetBytes(targetSize);

		await AssertRoundTrip(origin, target);
	}

	/// <summary>
	/// A target that reuses large runs of the origin (a head and a tail) with a fresh
	/// middle is the realistic delta shape: Create emits COPY commands for the shared
	/// runs and ':' INSERT commands for the new bytes, so the insert branch the fix
	/// touches is exercised against genuine, well-formed delta data.
	/// </summary>
	[TestMethod]
	public async Task RoundTrip_TargetDerivedFromOrigin()
	{
		var origin = RandomGen.GetBytes(8 * FileSizes.KB);

		var head = origin.Take(3000).ToArray();
		var tail = origin.Skip(5000).ToArray();
		var middle = RandomGen.GetBytes(700);

		var target = head.Concat(middle).Concat(tail).ToArray();

		await AssertRoundTrip(origin, target);
	}

	/// <summary>
	/// Fuzz: many random origin/target pairs of varied sizes must all round-trip. If the
	/// remaining-bytes bound were too strict it would reject some valid delta here.
	/// </summary>
	[TestMethod]
	public async Task RoundTrip_Fuzz()
	{
		for (var i = 0; i < 200; i++)
		{
			var originSize = RandomGen.GetInt(0, 4 * FileSizes.KB);
			var targetSize = RandomGen.GetInt(0, 4 * FileSizes.KB);

			var origin = RandomGen.GetBytes(originSize);

			// Half the time derive the target from the origin so deltas contain copies too.
			byte[] target;

			if (RandomGen.GetBool() && originSize > 64)
			{
				var cut = RandomGen.GetInt(0, originSize);
				target = origin.Take(cut)
					.Concat(RandomGen.GetBytes(RandomGen.GetInt(0, 256)))
					.Concat(origin.Skip(cut))
					.ToArray();
			}
			else
				target = RandomGen.GetBytes(targetSize);

			await AssertRoundTrip(origin, target);
		}
	}

	/// <summary>
	/// Corrupted/truncated real delta robustness: every prefix of a valid delta is invalid
	/// (it drops the trailing checksum/terminator), so Apply must reject each one cleanly -
	/// throw, never crash, hang or silently return a wrong-length result. Several of these
	/// truncations land inside a ':' insert with a count larger than the bytes left, which is
	/// exactly the out-of-bounds case the fix guards.
	/// </summary>
	[TestMethod]
	public async Task Apply_TruncatedValidDelta_AlwaysThrows()
	{
		var origin = RandomGen.GetBytes(2 * FileSizes.KB);

		var target = origin.Take(800)
			.Concat(RandomGen.GetBytes(300))
			.Concat(origin.Skip(1200))
			.ToArray();

		var delta = await Delta.Create(origin, target, CancellationToken);

		for (var len = 1; len < delta.Length; len++)
		{
			var truncated = delta.Take(len).ToArray();

			var threw = false;

			try
			{
				await Delta.Apply(origin, truncated, CancellationToken);
			}
			catch (Exception)
			{
				// Any structural rejection is fine; the point is that it neither reads out of
				// bounds nor hangs. A truncated fossil delta always loses its ';' terminator,
				// so a clean exception is the only correct outcome.
				threw = true;
			}

			threw.AssertTrue($"Truncated delta of length {len} must be rejected with an exception.");
		}
	}

	// Fossil base64-ish digit alphabet, matching the decoder in Reader.GetInt:
	// 0-9 => '0'-'9', 10-35 => 'A'-'Z', 36 => '_', 37-62 => 'a'-'z', 63 => '~'.
	private const string _fossilDigits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~";

	private static void PutInt(List<byte> delta, uint v)
	{
		if (v == 0)
		{
			delta.Add((byte)'0');
			return;
		}

		var digits = new List<byte>();

		while (v > 0)
		{
			digits.Insert(0, (byte)_fossilDigits[(int)(v & 63)]);
			v >>= 6;
		}

		delta.AddRange(digits);
	}

	/// <summary>
	/// A malformed delta whose ':' insert count is larger than the bytes remaining in the
	/// delta (but not larger than the whole delta) must be rejected, not read out of bounds.
	/// This reaches the insert branch from the public Apply, proving the guard is live:
	/// header limit=1000, a first 40-byte literal insert to advance the read position, then a
	/// second insert claiming 50 bytes with only 5 left.
	/// </summary>
	[TestMethod]
	public async Task Apply_InsertCountExceedsRemaining_Throws()
	{
		var delta = new List<byte>();

		PutInt(delta, 1000);            // output size / limit
		delta.Add((byte)'\n');

		PutInt(delta, 40);              // first insert: 40 literal bytes
		delta.Add((byte)':');
		delta.AddRange(Enumerable.Repeat((byte)'A', 40));

		PutInt(delta, 50);              // second insert claims 50 bytes...
		delta.Add((byte)':');
		delta.AddRange(Enumerable.Repeat((byte)'B', 5)); // ...but only 5 remain

		var ex = await ThrowsAsync<Exception>(async ()
			=> await Delta.Apply([], delta.ToArray(), CancellationToken));

		ex.Message.AssertEqual("insert count exceeds size of delta");
	}
}
