namespace Ecng.Common
{
	using System;

	public class DuplicateException : InvalidOperationException
	{
		public DuplicateException(string message) : base(message)
		{
		}
	}
}