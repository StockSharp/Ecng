namespace Ecng.Tests.Markdown;

using Ecng.Markdown;


[TestClass]
public class CounterTweenTests : BaseTestClass
{
	[TestMethod]
	public void Tween_LandsExactlyOnWhatTheAuthorWrote()
	{
		// The whole point of the count is the number it stops at. Arriving at "866" or "867.0" would make
		// the animation a way of getting the figure wrong.
		var tween = new CounterTween("867");

		tween.CanTick.AssertTrue();
		tween[0].AssertEqual("0");
		tween[1].AssertEqual("867");
		tween[2].AssertEqual("867");
	}

	[TestMethod]
	public void Tween_RunsForwardWithoutOvershooting()
	{
		var tween = new CounterTween("100");
		var previous = -1d;

		for (var progress = 0d; progress <= 1; progress += 0.1)
		{
			var value = tween[progress].To<double>();

			IsTrue(value >= previous);
			IsTrue(value <= 100);

			previous = value;
		}
	}

	[TestMethod]
	public void Tween_KeepsWhateverSurroundsTheNumber()
	{
		var tween = new CounterTween("27.0K");

		tween.CanTick.AssertTrue();
		tween[1].AssertEqual("27.0K");
		tween[0].AssertEqual("0.0K");
	}

	[TestMethod]
	public void Tween_FigureThatIsNotANumber_IsShownAsWritten()
	{
		// "Free" and "C# and Python" are figures too, and there is no half way between nothing and them.
		foreach (var text in new[] { "Free", "C# and Python", "" })
		{
			var tween = new CounterTween(text);

			tween.CanTick.AssertFalse();
			tween[0].AssertEqual(text);
			tween[1].AssertEqual(text);
		}
	}
}
