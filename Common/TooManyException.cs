namespace Ecng.Common;

using System;

public class TooManyException : InvalidOperationException
{
	public TooManyException(string message)
		: base(message)
	{
	}
}