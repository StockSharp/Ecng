namespace Ecng.Tests.Drawing;

using System.Runtime.InteropServices;
using System.Numerics;

using Ecng.Drawing;
using Ecng.Drawing.Codecs.Jpeg;

[TestClass]
public class JpegDctParityTests : BaseTestClass
{
	[TestMethod]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	public void Dct_MatchesThePortableKernelsBitForBit(bool inverse, bool offset)
	{
		var random = new Random(76231);
		for (var sample = 0; sample < 4096; sample++)
		{
			var source = new JpegBlock8x8F();
			var values = MemoryMarshal.CreateSpan(ref source.V0L.X, 64);
			for (var i = 0; i < values.Length; i++)
				values[i] = (float)(random.NextDouble() * 2048 - 1024);
			var portableSource = source;
			if (offset)
				portableSource.AddToAllInplace(new Vector4(-128));
			var expected = Portable(portableSource, inverse);
			var actual = new JpegBlock8x8F();
			var scratch = new JpegBlock8x8F();
			if (inverse)
				FastFloatingPointDCT.TransformIDCT(ref source, ref actual, ref scratch);
			else
				FastFloatingPointDCT.TransformFDCT(ref source, ref actual, ref scratch, offset);
			for (var i = 0; i < 64; i++)
				AreEqual(BitConverter.SingleToInt32Bits(expected[i]), BitConverter.SingleToInt32Bits(actual[i]), $"Block {sample}, coefficient {i}.");
		}
	}

	[TestMethod]
	public void QuantizedCoefficients_PreserveSignsTiesAndOverflowChecks()
	{
		var source = new float[64];
		var factors = new double[64];
		var actual = new short[64];
		for (var divisor = 1; divisor <= 255; divisor++)
		{
			for (var i = 0; i < 64; i++)
			{
				var value = ((i - 32) + 0.5f) * divisor;
				source[JpegTransforms.ZigZag[i]] = value;
				factors[i] = 1.0 / divisor;
			}
			JpegCoefficientQuantizer.Write(source, factors, actual);
			for (var i = 0; i < 64; i++)
			{
				var value = source[JpegTransforms.ZigZag[i]] * factors[i];
				AreEqual(checked((short)(int)(value + (value < 0 ? -0.5 : 0.5))), actual[i]);
			}
		}
		Array.Fill(factors, 1);
		source[0] = 32767.5f;
		ThrowsExactly<OverflowException>(() => JpegCoefficientQuantizer.Write(source, factors, actual));
		source[0] = -32768.5f;
		ThrowsExactly<OverflowException>(() => JpegCoefficientQuantizer.Write(source, factors, actual));
	}

	[TestMethod]
	public void QuantizedBlocks_MatchPortableDctAndDoubleRounding()
	{
		var random = new Random(69854);
		Span<double> samples = stackalloc double[64];
		Span<short> actual = stackalloc short[64];
		var factors = new double[64];
		for (var sample = 0; sample < 2048; sample++)
		{
			var source = new JpegBlock8x8F();
			for (var i = 0; i < 64; i++)
			{
				samples[i] = random.NextDouble() * 255 - 128;
				source[i] = (float)samples[i];
				factors[i] = 1.0 / random.Next(1, 256);
			}
			var transformed = Portable(source, false);
			JpegEncoder.QuantizeBlock(samples, factors, actual);
			for (var i = 0; i < 64; i++)
			{
				var value = transformed[JpegTransforms.ZigZag[i]] * factors[i];
				var expected = checked((short)(int)(value + (value < 0 ? -0.5 : 0.5)));
				AreEqual(expected, actual[i], $"Block {sample}, coefficient {i}.");
			}
		}
	}

	[TestMethod]
	public void ConstantAndNearlyConstantBlocks_MatchPortableQuantization()
	{
		var random = new Random(81736);
		var samples = new double[64];
		var actual = new short[64];
		var factors = new double[64];
		for (var sample = 0; sample < 4096; sample++)
		{
			var value = random.NextDouble() * 255 - 128;
			Array.Fill(samples, value);
			if (sample % 2 != 0)
				samples[sample % 64] += sample % 3 == 0 ? 1e-8 : 0.25;
			var source = new JpegBlock8x8F();
			for (var i = 0; i < 64; i++)
			{
				source[i] = (float)samples[i];
				factors[i] = 1.0 / random.Next(1, 256);
			}
			var expected = Portable(source, false);
			JpegEncoder.QuantizeBlock(samples, factors, actual);
			for (var i = 0; i < 64; i++)
			{
				var coefficient = expected[JpegTransforms.ZigZag[i]] * factors[i];
				AreEqual(checked((short)(int)(coefficient + (coefficient < 0 ? -0.5 : 0.5))), actual[i], $"Block {sample}, coefficient {i}.");
			}
		}
	}

	[TestMethod]
	public void IrregularFactors_MatchIndependentDoubleQuantization()
	{
		var random = new Random(88761);
		var source = new float[64];
		var factors = new double[64];
		var actual = new short[64];
		for (var block = 0; block < 4096; block++)
		{
			for (var i = 0; i < 64; i++)
			{
				source[i] = (float)(random.NextDouble() * 2048 - 1024);
				factors[i] = 1.0 / random.Next(1, 256);
			}
			JpegCoefficientQuantizer.Write(source, factors, actual);
			for (var i = 0; i < 64; i++)
			{
				var at = JpegTransforms.ZigZag[i];
				var value = source[at] * factors[i];
				AreEqual(checked((short)(int)(value + (value < 0 ? -0.5 : 0.5))), actual[i]);
			}
		}
		Array.Fill(factors, 1.0);
		source[0] = 32767.5f;
		ThrowsExactly<OverflowException>(() => JpegCoefficientQuantizer.Write(source, factors, actual));
		source[0] = -32768.5f;
		ThrowsExactly<OverflowException>(() => JpegCoefficientQuantizer.Write(source, factors, actual));
	}

	private static JpegBlock8x8F Portable(JpegBlock8x8F source, bool inverse)
	{
		var temporary = new JpegBlock8x8F();
		var destination = new JpegBlock8x8F();
		source.TransposeInto(ref temporary);
		if (inverse)
		{
			FastFloatingPointDCT.IDCT8x4_LeftPart(ref temporary, ref destination);
			FastFloatingPointDCT.IDCT8x4_RightPart(ref temporary, ref destination);
		}
		else
		{
			FastFloatingPointDCT.FDCT8x4_LeftPart(ref temporary, ref destination);
			FastFloatingPointDCT.FDCT8x4_RightPart(ref temporary, ref destination);
		}
		destination.TransposeInto(ref temporary);
		if (inverse)
		{
			FastFloatingPointDCT.IDCT8x4_LeftPart(ref temporary, ref destination);
			FastFloatingPointDCT.IDCT8x4_RightPart(ref temporary, ref destination);
		}
		else
		{
			FastFloatingPointDCT.FDCT8x4_LeftPart(ref temporary, ref destination);
			FastFloatingPointDCT.FDCT8x4_RightPart(ref temporary, ref destination);
		}
		destination.MultiplyInplace(0.125f);
		return destination;
	}
}
