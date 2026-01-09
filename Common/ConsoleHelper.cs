namespace Ecng.Common;

using System;
using System.Security;
using System.Threading;

/// <summary>
/// Provides helper methods for writing colored messages to the console and reading secure passwords.
/// </summary>
public static class ConsoleHelper
{
	/// <summary>
	/// Gets or sets the color used to display information messages.
	/// </summary>
	public static ConsoleColor Info = ConsoleColor.White;

	/// <summary>
	/// Gets or sets the color used to display warning messages.
	/// </summary>
	public static ConsoleColor Warning = ConsoleColor.Yellow;

	/// <summary>
	/// Gets or sets the color used to display error messages.
	/// </summary>
	public static ConsoleColor Error = ConsoleColor.Red;

	/// <summary>
	/// Gets or sets the color used to display success messages.
	/// </summary>
	public static ConsoleColor Success = ConsoleColor.Green;

	/// <summary>
	/// Writes an information message to the console.
	/// </summary>
	/// <param name="message">The message to write.</param>
	public static void ConsoleInfo(this string message)
	{
		Console.WriteLine(message);
	}

	/// <summary>
	/// Writes a warning message to the console using the predefined warning color.
	/// </summary>
	/// <param name="message">The warning message to write.</param>
	public static void ConsoleWarning(this string message)
	{
		message.ConsoleWithColor(Warning);
	}

	/// <summary>
	/// Writes an error message to the console using the predefined error color.
	/// </summary>
	/// <param name="message">The error message to write.</param>
	public static void ConsoleError(this string message)
	{
		message.ConsoleWithColor(Error);
	}

	/// <summary>
	/// Writes an exception to the console using the predefined error color.
	/// </summary>
	/// <param name="ex">The exception to write.</param>
	public static void ConsoleError(this Exception ex)
	{
		if (ex is null)
			return;

		ex.ToString().ConsoleWithColor(Error);
	}

	/// <summary>
	/// Writes a success message to the console using the predefined success color.
	/// </summary>
	/// <param name="message">The success message to write.</param>
	public static void ConsoleSuccess(this string message)
	{
		message.ConsoleWithColor(Success);
	}

	/// <summary>
	/// Writes a message to the console with the specified color.
	/// </summary>
	/// <param name="message">The message to write.</param>
	/// <param name="color">The color to use for the message.</param>
	public static void ConsoleWithColor(this string message, ConsoleColor color)
	{
		ConsoleWithColor(() => Console.WriteLine(message), color);
	}

	private static readonly Lock _lock = new();

	/// <summary>
	/// Executes the provided action while displaying console output in the specified color.
	/// </summary>
	/// <param name="handler">The action to execute.</param>
	/// <param name="color">The color to use for the console output.</param>
	/// <exception cref="ArgumentNullException">Thrown when the handler is null.</exception>
	public static void ConsoleWithColor(this Action handler, ConsoleColor color)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		using (_lock.EnterScope())
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

	/// <summary>
	/// Reads a password from the console without displaying it, outputting a masked version.
	/// </summary>
	/// <returns>A <see cref="SecureString"/> that contains the password.</returns>
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
