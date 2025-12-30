namespace Ecng.Tests.IO;

using Ecng.IO.Fossil;

using Ecng.IO.Compression;

[TestClass]
public class FossilTests : BaseTestClass
{
	[TestMethod]
	public async Task Diff()
	{
		var token = CancellationToken;

		var bytes1 = RandomGen.GetBytes(FileSizes.MB);
		var bytes2 = RandomGen.GetBytes(FileSizes.MB);

		var delta = await Delta.Create(bytes1, bytes2, token);

		(await Delta.Apply(bytes1, delta, token)).AssertEqual(bytes2);
	}
}
