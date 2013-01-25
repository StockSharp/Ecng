namespace Ecng.Common
{
	using System;

	public static class ConsoleHelper
	{
		public static ConsoleColor Info = ConsoleColor.White;
		public static ConsoleColor Warning = ConsoleColor.Yellow;
		public static ConsoleColor Error = ConsoleColor.Red;
		public static ConsoleColor Success = ConsoleColor.Green;

		public static void ConsoleInfo(this string message)
		{
			Console.WriteLine(message);
		}

		public static void ConsoleWarning(this string message)
		{
			message.ConsoleWithColor(Warning);
		}

		public static void ConsoleError(this string message)
		{
			message.ConsoleWithColor(Error);
		}

		public static void ConsoleSuccess(this string message)
		{
			message.ConsoleWithColor(Success);
		}

		public static void ConsoleWithColor(this string message, ConsoleColor color)
		{
			ConsoleWithColor(() => Console.WriteLine(message), color);
		}

		public static void ConsoleWithColor(this Action handler, ConsoleColor color)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			var prevColor = Console.ForegroundColor;

			Console.ForegroundColor = color;

			try
			{
				handler();
			}
			finally
			{
				Console.ForegroundColor = prevColor;
			}
		}
	}
}