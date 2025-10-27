namespace Ecng.Tests.Common;

[TestClass]
public class WatchTests
{
	[TestMethod]
	public void NullAction()
	{
		// Arrange
		Action action = null;

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => Watch.Do(action), "expected ArgumentNullException when action is null");
	}

	[TestMethod]
	public void ValidAction()
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
		executed.AssertTrue("action was not executed");
		(elapsed.TotalMilliseconds >= 10).AssertTrue("elapsed.TotalMilliseconds should be >=10");
		(elapsed.TotalMilliseconds < 1000).AssertTrue("elapsed.TotalMilliseconds should be <1000");
	}

	[TestMethod]
	public void EmptyAction()
	{
		// Arrange
		static void action()
		{ }

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		(elapsed.TotalMilliseconds >= 0).AssertTrue("elapsed.TotalMilliseconds should be >=0");
		(elapsed.TotalSeconds < 1).AssertTrue("elapsed.TotalSeconds should be <1");
	}

	[TestMethod]
	public void ActionThrowsException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test exception");
		void action() => throw expectedException;

		// Act & Assert
		var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => Watch.Do(action), "ActionThrowsException: expected InvalidOperationException");
		thrown.Message.AssertEqual("test exception", "exception message mismatch");
	}

	[TestMethod]
	public void ActionWith100msDelay()
	{
		// Arrange
		static void action() => Thread.Sleep(100);

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		(elapsed.TotalMilliseconds >= 100).AssertTrue($"elapsed should be >=100 ms but {(int)elapsed.TotalMilliseconds}");
		(elapsed.TotalMilliseconds <= 200).AssertTrue($"elapsed should be <=200 ms but {(int)elapsed.TotalMilliseconds}");
	}
}
