namespace Ecng.Tests.Interop;

using Ecng.Interop;

[TestClass]
public class HardwareInfoTests
{
	[TestMethod]
	public async Task HddId()
	{
		if (OperatingSystemEx.IsMacOS())
			return;

		((await HardwareInfo.GetIdAsync()).Length > 10).AssertTrue();
	}
}