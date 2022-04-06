namespace Ecng.Tests.Security
{
	using System.IO;

	using Ecng.Common;
	using Ecng.Security;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SecretTests
	{
		private const string _correctPwd = "mpoi3e/4nn3(&T(*^R";
		private const string _incorrectPwd = "mpo3e/4nn3(&T(*^R";

		[TestMethod]
		public void EqualsTest()
		{
			var pwd1 = _correctPwd.CreateSecret();
			var pwd2 = _correctPwd.CreateSecret();

			pwd1.Equals(pwd1).AssertTrue();
			pwd1.Equals(pwd2).AssertFalse();

			pwd1.IsValid(_correctPwd).AssertTrue();
			pwd2.IsValid(_correctPwd).AssertTrue();
		}

		[TestMethod]
		public void NonEqualsTest()
		{
			var pwd1 = _correctPwd.CreateSecret();
			var pwd2 = _incorrectPwd.CreateSecret();

			pwd1.Equals(pwd2).AssertFalse();
		}

		[TestMethod]
		public void IsValidTest()
		{
			_correctPwd.CreateSecret().IsValid(_correctPwd).AssertTrue();
			_correctPwd.CreateSecret().IsValid(_incorrectPwd).AssertFalse();
		}

		[TestMethod]
		public void SecureStringTest()
		{
			var correctPwd = _correctPwd.Secure();
			var incorrectPwd = _incorrectPwd.Secure();

			correctPwd.CreateSecret().IsValid(correctPwd).AssertTrue();
			correctPwd.CreateSecret().IsValid(incorrectPwd).AssertFalse();
		}

		[TestMethod]
		public void DecryptReadStreamTest()
		{
	        var initVectorBytes = "ss14fgty650h8u82".ASCII();
	        var txt = File.ReadAllText("encrypted_config").Base64().Decrypt("qwerty", initVectorBytes, initVectorBytes).UTF8();

			txt.Length.AssertEqual(65735);
			txt.UTF8().Md5().AssertEqual("96FB3B2D0226B0A18903E929E1238557");
		}
	}
}