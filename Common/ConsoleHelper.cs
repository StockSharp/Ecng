namespace Ecng.Common
{
	using System;
	using System.Security;

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

		private static readonly SyncObject _lock = new();

		public static void ConsoleWithColor(this Action handler, ConsoleColor color)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			lock (_lock)
			{
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

		public static SecureString ReadPassword()
		{
			var pass = new SecureString();
			ConsoleKeyInfo key;

			do
			{
				key = Console.ReadKey(true);

				if (!char.IsControl(key.KeyChar))
				{
					pass.AppendChar(key.KeyChar);
					Console.Write("*");
				}
				else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
				{
					pass.RemoveAt(pass.Length - 1);
					Console.Write("\b \b");
				}
			}
			while (key.Key != ConsoleKey.Enter);

			return pass;
		}
	}
}