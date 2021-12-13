namespace Ecng.Tests.Net
{
	using System;
	using System.Net;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Net.Captcha;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CaptchaTests
	{

		[TestMethod]
		public async Task Simple()
		{
			ICaptchaValidator<float> validator = new ReCaptcha3Validator(Config.HttpClient, "123".Secure());

			try
			{
				await validator.ValidateAsync("123", IPAddress.Loopback.To<string>());
			}
			catch (InvalidOperationException ex)
			{
				ex.Message.AssertEqual("invalid-input-secret");
			}
		}
	}
}
