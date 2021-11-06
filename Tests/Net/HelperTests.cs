namespace Ecng.Tests.Net
{
	using Ecng.Net;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class HelperTests
	{
		[TestMethod]
		public void Gravatar()
		{
			"info@stocksharp.com".GetGravatarUrl(100).AssertEqual("https://www.gravatar.com/avatar/cf4c4e682b9869e05c4cc4536b734828?size=100");
		}
	}
}
