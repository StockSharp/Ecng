namespace Ecng.Tests.Interop;

using Ecng.Interop;

[TestClass]
public class HardwareInfoTests : BaseTestClass
{
	[TestMethod]
	public async Task HddId()
	{
		var id = await HardwareInfo.GetIdAsync(CancellationToken);
		(id.Length > 10).AssertTrue($"id='{id}'.Length should be > 10");
	}
}