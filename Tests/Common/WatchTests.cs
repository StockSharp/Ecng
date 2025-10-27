namespace Ecng.Tests.Common;

[TestClass]
public class WatchTests
{
	[TestMethod]
	public void Do_NullAction_ThrowsArgumentNullException()
	{
		// Arrange
		Action action = null;

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => Watch.Do(action));
	}

	[TestMethod]
	public void Do_ValidAction_ReturnsElapsedTime()
	{
		// Arrange
		var executed = false;
		void action()
		{
			Thread.Sleep(10);
			executed = true;
		}

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		executed.AssertTrue();
		(elapsed.TotalMilliseconds >= 10).AssertTrue();
		(elapsed.TotalMilliseconds < 1000).AssertTrue();
	}

	[TestMethod]
	public void Do_EmptyAction_ReturnsVerySmallElapsedTime()
	{
		// Arrange
		static void action()
		{ }

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		(elapsed.TotalMilliseconds >= 0).AssertTrue();
		(elapsed.TotalSeconds < 1).AssertTrue();
	}

	[TestMethod]
	public void Do_ActionThrowsException_PropagatesException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test exception");
		void action() => throw expectedException;

		// Act & Assert
		var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => Watch.Do(action));
		thrown.Message.AssertEqual("test exception");
	}

	[TestMethod]
	public void Do_ActionWith100msDelay_ReturnsApproximately100ms()
	{
		// Arrange
		static void action() => Thread.Sleep(100);

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		(elapsed.TotalMilliseconds >= 100).AssertTrue();
		(elapsed.TotalMilliseconds <= 200).AssertTrue(); // Allow some tolerance
	}
}
