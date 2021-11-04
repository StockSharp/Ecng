namespace Ecng.Tests.Interop
{
	using System.Threading.Tasks;

	using Ecng.UnitTesting;
	using Ecng.Interop;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class HardwareInfoTests
	{
		[TestMethod]
		public async Task HddId()
		{
			((await HardwareInfo.GetIdAsync()).Length > 10).AssertTrue();
		}
	}
}