namespace Ecng.Tests.IO
{
	using System.Linq;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class FossilTests
	{
		[TestMethod]
		public void Diff()
		{
			var bytes1 = RandomGen.GetBytes(1024 * 1024);
			var bytes2 = RandomGen.GetBytes(1024 * 1024);

			var delta = Fossil.Delta.Create(bytes1, bytes2);

			Fossil.Delta.Apply(bytes1, delta).SequenceEqual(bytes2).AssertTrue();
		}
	}
}
