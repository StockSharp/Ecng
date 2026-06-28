namespace Ecng.Tests.IO;

using System.Linq;
using System.Threading.Tasks;

using Ecng.IO.Compression;

[TestClass]
public class CompressionGZipTests : BaseTestClass
{
	[TestMethod]
	public async Task GZipRoundtrip()
	{
		var data = Enumerable.Range(0, 2000).Select(i => (byte)(i % 256)).ToArray();

		var compressed = await data.GZipAsync(default);
		var back = await compressed.UnGZipAsync(default);

		back.SequenceEqual(data).AssertTrue();
	}
}
