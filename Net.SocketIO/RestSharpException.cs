namespace Ecng.Net;

public class RestSharpException : InvalidOperationException
{
    public RestSharpException(string message, RestResponse response)
		: base(message)
    {
		Response = response ?? throw new ArgumentNullException(nameof(response));
	}

	public RestResponse Response { get; }
}