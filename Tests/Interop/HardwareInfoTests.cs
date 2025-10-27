namespace Ecng.Tests.Interop;

using Ecng.Interop;

[TestClass]
public class HardwareInfoTests : BaseTestClass
{
	[TestMethod]
	public async Task HddId()
	{
		((await HardwareInfo.GetIdAsync(CancellationToken)).Length > 10).AssertTrue();
	}
}