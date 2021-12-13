namespace Ecng.Net.Captcha
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface ICaptchaValidator<TResult>
	{
		Task<TResult> ValidateAsync(string response, string address, CancellationToken cancellationToken = default);
	}
}