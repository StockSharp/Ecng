namespace Ecng.Net;

public class RestSharpException(string message, RestResponse response) : InvalidOperationException(message)
{
	public RestResponse Response { get; } = response ?? throw new ArgumentNullException(nameof(response));
}