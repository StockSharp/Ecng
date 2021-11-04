namespace Ecng.Tests.Reflection
{
	using System.IO;

	using Ecng.Common;
	using Ecng.Reflection.Emit;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class AssemblyCacheTest
	{
		private const string _path = "asm_cache";

		[ClassCleanup]
		public static void Cleanup()
		{
			Directory.Delete(_path, true);
		}

		[TestMethod]
		public void Test1()
		{
			using var _ = new Scope<AssemblyHolderSettings>(new AssemblyHolderSettings { AssemblyCachePath = _path });
			new MemberInvokeTest().InvokeReturnMethodWithParams7();
			(Directory.GetFiles(_path).Length > 0).AssertTrue();
		}
	}
}
