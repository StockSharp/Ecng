namespace Ecng.Test.ComponentModel
{
	using System;
	
	using Ecng.Common;
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

			string GetValue()
				=> (++invokeCount).To<string>();

			var lazy = new Lazy<string>(GetValue);
			lazy.Track();
			lazy.Value.AssertEqual("1");
			lazy.Value.AssertEqual("1");

			lazy.Reset();
			lazy.Value.AssertEqual("2");
		}
	}
}
