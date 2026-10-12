namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

[TestClass]
public class JpegEntropyTests : BaseTestClass
{
	[TestMethod]
	public void ShortHuffmanCodeAtScanEndDoesNotConsumeTheMarker()
	{
		byte[] counts = [1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		var table = new JpegHuffmanTable(counts, [42, 73]);
		var reader = new JpegBitReader([0x00, 0xff, 0xd9], 0);
		AreEqual(0, reader.Read(7));
		AreEqual(42, table.Decode(reader));
		AreEqual(1, reader.Position);
		ThrowsExactly<InvalidDataException>(() => reader.ReadBit());
	}

	[TestMethod]
	public void LongHuffmanCodeUsesTheRemainingBitsAcrossBytes()
	{
		byte[] counts = [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0];
		var table = new JpegHuffmanTable(counts, [91]);
		var reader = new JpegBitReader([0x00, 0x7f, 0xff, 0xd9], 0);
		AreEqual(91, table.Decode(reader));
		AreEqual(2, reader.Position);
	}

	[TestMethod]
	public void StuffedBytesAndRestartMarkersPreserveEntropyBits()
	{
		var reader = new JpegBitReader([0xff, 0x00, 0xab, 0xff, 0xd0, 0xcd, 0xff, 0xd9], 0);
		AreEqual(0xffab, reader.Read(16));
		reader.Restart(0);
		AreEqual(0xcd, reader.Read(8));
		AreEqual(6, reader.Position);
	}

	[TestMethod]
	public void RestartRejectsUnconsumedEntropyBytes()
	{
		byte[] counts = [1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		var table = new JpegHuffmanTable(counts, [42, 73]);
		var reader = new JpegBitReader([0, 0, 0xff, 0xd0], 0);
		for (var i = 0; i < 7; i++) AreEqual(42, table.Decode(reader));
		ThrowsExactly<InvalidDataException>(() => reader.Restart(0));
	}

	[TestMethod]
	public void MixedLongCodes_PreserveShortTailsAndRejectUnassignedWords()
	{
		var counts = new byte[16];
		counts[8] = counts[15] = 1;
		var table = new JpegHuffmanTable(counts, [42, 73]);
		var shortTail = new JpegBitReader([0, 0, 0xff, 0xd9], 0);
		AreEqual(0, shortTail.Read(7));
		AreEqual(42, table.Decode(shortTail));
		AreEqual(2, shortTail.Position);
		ThrowsExactly<InvalidDataException>(() => shortTail.ReadBit());
		var longWord = new JpegBitReader([0, 0x80, 0xff, 0xd9], 0);
		AreEqual(73, table.Decode(longWord));
		AreEqual(2, longWord.Position);
		ThrowsExactly<InvalidDataException>(() => table.Decode(new JpegBitReader([0, 0x81, 0xff, 0xd9], 0)));
	}

	[TestMethod]
	public void CombinedSymbolsAndMagnitudes_PreserveSignsStuffingAndShortTails()
	{
		foreach (var codeLength in new[] { 1, 8, 9, 16 })
			for (var magnitude = 0; magnitude <= 15; magnitude++)
				foreach (var encoded in new[] { 0, (1 << magnitude) / 2, (1 << magnitude) - 1 }.Distinct())
					foreach (var extraBytes in new[] { 0, 4 })
					{
						var counts = new byte[16];
						counts[codeLength - 1] = 1;
						var table = new JpegHuffmanTable(counts, [(byte)magnitude]);
						var text = new string('0', codeLength) + (magnitude == 0 ? string.Empty : Convert.ToString(encoded, 2).PadLeft(magnitude, '0'));
						text = text.PadRight((text.Length + 7) / 8 * 8 + extraBytes * 8, '0');
						var data = new List<byte>();
						for (var at = 0; at < text.Length; at += 8)
						{
							var value = Convert.ToByte(text.Substring(at, 8), 2);
							data.Add(value);
							if (value == 255) data.Add(0);
						}
						data.AddRange([0xff, 0xd9]);
						var reader = new JpegBitReader(data.ToArray(), 0);
						AreEqual(magnitude, table.DecodeValue(reader, out var actual));
						var expected = magnitude == 0 ? 0 : encoded < 1 << (magnitude - 1) ? encoded - ((1 << magnitude) - 1) : encoded;
						AreEqual(expected, actual);
						for (var left = text.Length - codeLength - magnitude; left > 0;)
						{
							var count = Math.Min(16, left);
							AreEqual(0, reader.Read(count));
							left -= count;
						}
						AreEqual(data.Count - 2, reader.Position);
						ThrowsExactly<InvalidDataException>(() => table.DecodeValue(reader, out _));
					}
	}
}
