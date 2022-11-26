namespace Ecng.Net.Captcha;

public interface ICaptchaValidator<TResult>
{
	Task<TResult> ValidateAsync(string response, string address, CancellationToken cancellationToken = default);
}