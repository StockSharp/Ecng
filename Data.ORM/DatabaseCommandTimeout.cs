namespace Ecng.Data;

public class DatabaseCommandTimeout(TimeSpan timeout)
{
	public TimeSpan Timeout { get; } = timeout;
}