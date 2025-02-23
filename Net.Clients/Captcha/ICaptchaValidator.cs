namespace Ecng.Net.Captcha;

/// <summary>
/// Provides functionality to validate captcha responses.
/// </summary>
/// <typeparam name="TResult">The type of the validation result returned.</typeparam>
public interface ICaptchaValidator<TResult>
{
	/// <summary>
	/// Validates the captcha response asynchronously.
	/// </summary>
	/// <param name="response">The captcha response provided by the user.</param>
	/// <param name="address">The IP address associated with the captcha request.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous validation operation with a result of type <typeparamref name="TResult"/>.</returns>
	Task<TResult> ValidateAsync(string response, string address, CancellationToken cancellationToken = default);
}