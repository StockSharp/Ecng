namespace Ecng.Tests.ComponentModel
{
	using System;
	
	using Ecng.ComponentModel;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	}
}