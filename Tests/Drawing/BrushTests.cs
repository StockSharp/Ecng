namespace Ecng.Tests.Drawing;

using System.Drawing;

using Ecng.Drawing;

[TestClass]
public class BrushTests : BaseTestClass
{
	[TestMethod]
	public void LinearGradientBrush_ReversedStops_RectIsBoundingBox()
	{
		// stop1 is above-left of stop0: the rectangle must be the bounding box of both stops,
		// not one anchored at stop0 (which would land in the wrong quadrant).
		var brush = new LinearGradientBrush(new Point(100, 100), new Point(0, 0), Color.Red, Color.Blue);

		brush.Rectangle.AssertEqual(new Rectangle(0, 0, 100, 100));
	}

	[TestMethod]
	public void LinearGradientBrush_ForwardStops_RectIsBoundingBox()
	{
		var brush = new LinearGradientBrush(new Point(0, 0), new Point(100, 100), Color.Red, Color.Blue);

		brush.Rectangle.AssertEqual(new Rectangle(0, 0, 100, 100));
	}
}
