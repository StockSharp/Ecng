namespace Ecng.Common
{
	using System;

	public class DuplicateException(string message) : InvalidOperationException(message)
	{
	}
}