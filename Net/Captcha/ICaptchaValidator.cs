namespace Ecng.Net.Captcha
{
	using System;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ICaptchaValidator : IDisposable
	{
		Task<float> Verify(string response, IPAddress remoteip, CancellationToken cancellationToken = default);
	}
}