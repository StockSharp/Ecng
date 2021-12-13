namespace Ecng.Net.Captcha
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ICaptchaValidator<TResult> : IDisposable
	{
		Task<TResult> ValidateAsync(string response, string address, CancellationToken cancellationToken = default);
	}
}