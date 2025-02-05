namespace Ecng.Tests.IO;

using System.Threading;

using Ecng.IO.Fossil;

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
