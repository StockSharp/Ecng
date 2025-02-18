namespace Ecng.Net;

/// <summary>
/// Represents an exception that is thrown when a RestSharp operation fails.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="response">
/// The <see cref="RestResponse"/> associated with the failed RestSharp operation.
/// </param>
public class RestSharpException(string message, RestResponse response) : InvalidOperationException(message)
{
	/// <summary>
	/// Gets the response returned from the RestSharp call that caused the exception.
	/// </summary>
	public RestResponse Response { get; } = response ?? throw new ArgumentNullException(nameof(response));
}