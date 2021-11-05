namespace Ecng.Tests.IO
{
	using System.Linq;

	using Ecng.Common;
	using Ecng.IO;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class OctodiffTests
	{
		[TestMethod]
		public void Diff()
		{
			var bytes1 = RandomGen.GetBytes(1024 * 1024);
			var bytes2 = RandomGen.GetBytes(1024 * 1024);

			var sig = bytes1.CreateSignature();

			sig.SequenceEqual(bytes1.CreateSignature()).AssertTrue();
			sig.CreateOriginal(sig.CreateDelta(bytes2)).SequenceEqual(bytes2).AssertTrue();
		}
	}
}
