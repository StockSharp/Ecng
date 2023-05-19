namespace Ecng.Common;

using System;

public class ExpiredException : InvalidOperationException
{
	public ExpiredException(string message)
		: base(message)
	{
	}
}