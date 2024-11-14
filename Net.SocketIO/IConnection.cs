namespace Ecng.Net;

public interface IConnection
{
	event Action<ConnectionStates> StateChanged;

	ValueTask Connect(CancellationToken cancellationToken);
	void Disconnect();
}