namespace Ecng.Tests.ComponentModel;

using Ecng.Reflection;

[TestClass]
public class LazyTests
{
	[TestMethod]
	public void TrackReset()
	{
		var invokeCount = 0;

		int GetValue()
			=> ++invokeCount;

		var lazy = new Lazy<int>(GetValue);
		lazy.Track();
		lazy.Value.AssertEqual(1);
		lazy.Value.AssertEqual(1);

		lazy.Reset();
		lazy.Value.AssertEqual(2);
	}

	[TestMethod]
	public void SetValue()
	{
		var lazy = new Lazy<int>(() => 42);
		lazy.Track();
		lazy.SetValue(100);
		lazy.Value.AssertEqual(100);
	}

	[TestMethod]
	public void Untrack()
	{
		var invokeCount = 0;
		var lazy = new Lazy<int>(() => ++invokeCount);
		lazy.Track();
		lazy.Value.AssertEqual(1);
		lazy.Reset();
		lazy.Value.AssertEqual(2);
		lazy.Untrack();
		Assert.ThrowsExactly<KeyNotFoundException>(() => lazy.Reset());
		lazy.Value.AssertEqual(2);
	}
}