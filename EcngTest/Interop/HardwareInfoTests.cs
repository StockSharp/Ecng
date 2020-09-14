namespace Ecng.Test.Interop
{
	using Ecng.UnitTesting;
	using Ecng.Interop;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class HardwareInfoTests
	{
		[TestMethod]
		public void HddId()
		{
			(HardwareInfo.Instance.Id.Length > 10).AssertTrue();
		}
	}
}