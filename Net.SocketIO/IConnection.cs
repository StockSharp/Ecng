namespace Ecng.Net;

public interface IConnection
{
	event Action<ConnectionStates> StateChanged;
}