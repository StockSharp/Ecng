namespace Ecng.Common
{
	using System;

	public class ForbiddenException(string message) : InvalidOperationException(message)
	{
	}
}