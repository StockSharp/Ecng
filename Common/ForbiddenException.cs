namespace Ecng.Common
{
	using System;

	public class ForbiddenException : InvalidOperationException
	{
		public ForbiddenException(string message) : base(message)
		{
		}
	}
}