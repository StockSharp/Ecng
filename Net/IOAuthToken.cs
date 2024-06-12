namespace Ecng.Net;

public interface IOAuthToken
{
	string Value { get; }
	DateTime? Expires { get; }
}