namespace Ecng.Tests.IO
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.UnitTesting;
	using Ecng.IO.Fossil;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class FossilTests
	{
		[TestMethod]
		public async Task Diff()
		{
			var token = CancellationToken.None;

			var bytes1 = RandomGen.GetBytes(FileSizes.MB);
			var bytes2 = RandomGen.GetBytes(FileSizes.MB);

			var delta = await Delta.Create(bytes1, bytes2, token);

			(await Delta.Apply(bytes1, delta, token)).SequenceEqual(bytes2).AssertTrue();
		}
	}
}
