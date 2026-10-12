namespace Ecng.Tests.Drawing;

using System.Runtime.InteropServices;

using Ecng.Drawing.Codecs.Jpeg;

[TestClass]
public class JpegBlockTransformTests : BaseTestClass
{
	[TestMethod]
	public void Transpose_PreservesEverySampleBitIncludingNonFiniteValues()
	{
		var source = new JpegBlock8x8F();
		var destination = new JpegBlock8x8F();
		var values = MemoryMarshal.CreateSpan(ref source.V0L.X, 64);
		var actual = MemoryMarshal.CreateSpan(ref destination.V0L.X, 64);
		for (var pattern = 0; pattern < 4096; pattern++)
		{
			for (var i = 0; i < 64; i++) values[i] = BitConverter.Int32BitsToSingle(unchecked((int)((uint)pattern * 2654435761u + (uint)i * 2246822519u)));
			source.TransposeInto(ref destination);
			for (var y = 0; y < 8; y++)
				for (var x = 0; x < 8; x++) AreEqual(BitConverter.SingleToInt32Bits(values[x * 8 + y]), BitConverter.SingleToInt32Bits(actual[y * 8 + x]));
		}
	}
}
