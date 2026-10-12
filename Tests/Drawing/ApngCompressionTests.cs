namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class ApngCompressionTests : BaseTestClass
{
	[TestMethod]
	public void Conversion_StoresChangedRectangleAndPreservesEveryDisplayedFrame()
	{
		var builder = new GifBuilder(128, 96, GifBuilder.GrayPalette(2));
		builder.Frame(0, 0, 128, 96, new byte[128 * 96], delay: 4);
		builder.Frame(32, 20, 2, 2, [1, 1, 1, 1], delay: 9);
		builder.Frame(32, 20, 2, 2, [1, 1, 1, 1], delay: 13);
		var input = builder.Build();
		var output = input.ConvertToPng();
		var controls = ReadChunks(output).Where(chunk => chunk.tag == "fcTL").Select(chunk => chunk.data).ToArray();
		AreEqual(3, controls.Length);
		AreEqual(2, BinaryPrimitives.ReadInt32BigEndian(controls[1].AsSpan(4)));
		AreEqual(2, BinaryPrimitives.ReadInt32BigEndian(controls[1].AsSpan(8)));
		AreEqual(32, BinaryPrimitives.ReadInt32BigEndian(controls[1].AsSpan(12)));
		AreEqual(20, BinaryPrimitives.ReadInt32BigEndian(controls[1].AsSpan(16)));
		AreEqual(1, BinaryPrimitives.ReadInt32BigEndian(controls[2].AsSpan(4)));
		var expected = input.GetAnimationFrames();
		var actual = ReadApng(output).Frames;
		AreEqual(expected.Count, actual.Count);
		var canvas = new byte[128 * 96 * 4];
		for (var frame = 0; frame < expected.Count; frame++)
		{
			var rect = actual[frame];
			AreEqual((byte)0, rect.Disposal);
			AreEqual((byte)0, rect.Blend);
			for (var y = 0; y < rect.Height; y++)
				rect.Rgba.AsSpan(y * rect.Width * 4, rect.Width * 4).CopyTo(canvas.AsSpan(((rect.Top + y) * 128 + rect.Left) * 4));
			AssertPixels(ReadPng(expected[frame].png, 128, 96), canvas, 128);
			AreEqual(expected[frame].delayMilliseconds, rect.DelayNumerator * 1000 / rect.DelayDenominator);
		}
	}
}
