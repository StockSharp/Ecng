namespace Ecng.Common;

using System;

public class LockedException : InvalidOperationException
{
	public LockedException(string message)
		: base(message)
	{
	}
}
