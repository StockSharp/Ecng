namespace Ecng.Localization
{
	using System;

	public class LocalizationException : ApplicationException
	{
		public LocalizationException(string message)
			: base(message)
		{
		}
	}
}