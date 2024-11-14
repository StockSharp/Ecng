namespace Ecng.Net;

public interface IConnection
{
	event Action<ConnectionStates> StateChanged;

	ValueTask ConnectAsync(CancellationToken cancellationToken);
	void Disconnect();
}