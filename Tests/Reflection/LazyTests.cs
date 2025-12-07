#pragma warning disable CS0618 // Type or member is obsolete
namespace Ecng.Tests.Reflection;

using Ecng.Reflection;

[TestClass]
public class LazyTests : BaseTestClass
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
		ThrowsExactly<KeyNotFoundException>(() => lazy.Reset());
		lazy.Value.AssertEqual(2);
	}
}
#pragma warning restore CS0618 // Type or member is obsolete