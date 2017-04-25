namespace Ecng.Test.Common
{
	using System;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class StringTests
	{
		[TestMethod]
		public void ReplaceIgnoreCase()
		{
			"".ReplaceIgnoreCase("", "").AssertEqual("");
			"1".ReplaceIgnoreCase("1", "2").AssertEqual("2");
			"1".ReplaceIgnoreCase("2", "1").AssertEqual("1");
			"".ReplaceIgnoreCase("1", "2").AssertEqual("");
			"1".ReplaceIgnoreCase("11", "22").AssertEqual("1");
			"1".ReplaceIgnoreCase("", "22").AssertEqual("1");
			"".ReplaceIgnoreCase("", "22").AssertEqual("22");
			((string)null).ReplaceIgnoreCase("", "22").AssertEqual(null);

			"AA ffgg GGG".ReplaceIgnoreCase("g", "k").AssertEqual("AA ffkk kkk");
			"AA ffgg GGG".ReplaceIgnoreCase("g", "").AssertEqual("AA ff ");

			"AbABabab".ReplaceIgnoreCase("ab", "kg").AssertEqual("kgkgkgkg");
			"AbABaba".ReplaceIgnoreCase("ab", "kg").AssertEqual("kgkgkga");

			"_".ReplaceIgnoreCase("_", "/").AssertEqual("/");
			"__".ReplaceIgnoreCase("_", "/").AssertEqual("//");
			"___".ReplaceIgnoreCase("__", "/").AssertEqual("/_");
			"___S".ReplaceIgnoreCase("__", "/").AssertEqual("/_S");
			"S___S".ReplaceIgnoreCase("__", "/").AssertEqual("S/_S");
			"S___".ReplaceIgnoreCase("__", "/").AssertEqual("S/_");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ReplaceIgnoreCaseError()
		{
			((string)null).ReplaceIgnoreCase(null, "22");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ReplaceIgnoreCaseError2()
		{
			"".ReplaceIgnoreCase(null, "22");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ReplaceIgnoreCaseError3()
		{
			"".ReplaceIgnoreCase("", null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ReplaceIgnoreCaseError4()
		{
			"11".ReplaceIgnoreCase("11", null);
		}
	}
}