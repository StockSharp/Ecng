namespace Ecng.Tests.Drawing;

/// <summary>
/// An animated PNG as its chunks describe it, read by <see cref="ImageTestHelper.ReadApng"/>.
/// </summary>
internal sealed class ApngFile
{
	/// <summary>
	/// One frame: its rectangle, timing and pixels, not composited with the others.
	/// </summary>
	public sealed class Frame
	{
		public int Width { get; init; }
		public int Height { get; init; }
		public int Left { get; init; }
		public int Top { get; init; }
		public ushort DelayNumerator { get; init; }
		public ushort DelayDenominator { get; init; }
		public byte Disposal { get; init; }
		public byte Blend { get; init; }
		public byte[] Rgba { get; set; }
	}

	public int Width { get; init; }
	public int Height { get; init; }
	public int FrameCount { get; init; }

	/// <summary>
	/// The num_plays field: how many times the animation runs, 0 for endlessly.
	/// </summary>
	public int Plays { get; init; }

	public List<Frame> Frames { get; } = [];
}
