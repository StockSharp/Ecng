namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;

	using Ecng.Common;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	public class FileSizeFormatProvider : IFormatProvider, ICustomFormatter
	{
		#region Private Fields

		private const string _fileSizeFormat = "fs";

		private static readonly string[] _letters = new[] { "b", "kb", "mb", "gb", "tb", "pb" };

		#endregion

		#region IFormatProvider Members

		/// <summary>
		/// Gets an object that provides formatting services for the specified type.
		/// </summary>
		/// <param name="formatType">An object that specifies the type of format object to get.</param>
		/// <returns>
		/// The current instance, if formatType is the same type as the current instance; otherwise, null.
		/// </returns>
		object IFormatProvider.GetFormat(Type formatType)
		{
			return formatType.Is<ICustomFormatter>() ? this : null;
		}

		#endregion

		#region ICustomFormatter Members

		/// <summary>
		/// Converts the value of a specified object to an equivalent string representation using specified format and culture-specific formatting information.
		/// </summary>
		/// <param name="format">A format string containing formatting specifications.</param>
		/// <param name="arg">An object to format.</param>
		/// <param name="formatProvider">An <see cref="T:System.IFormatProvider"></see> object that supplies format information about the current instance.</param>
		/// <returns>
		/// The string representation of the value of arg, formatted as specified by format and formatProvider.
		/// </returns>
		string ICustomFormatter.Format(string format, object arg, IFormatProvider formatProvider)
		{
			if (format is null || !format.StartsWith(_fileSizeFormat))
			{
				return DefaultFormat(format, arg, formatProvider);
			}

			decimal size;

			try
			{
				size = arg.To<decimal>();
			}
			catch (InvalidCastException)
			{
				return DefaultFormat(format, arg, formatProvider);
			}

			byte i = 0;
			while ((size >= FileSizes.KB) && (i < _letters.Length - 1))
			{
				i++;
				size /= FileSizes.KB;
			}

			var precision = format.Substring(2);

			if (precision.IsEmpty())
				precision = "2";

			return ("{0:N" + precision + "}{1}").Put(size, _letters[i]);
		}

		#endregion

		#region DefaultFormat

		private static string DefaultFormat(string format, object arg, IFormatProvider formatProvider)
		{
			return arg is IFormattable formattableArg ? formattableArg.ToString(format, formatProvider) : arg.ToString();
		}

		#endregion
	}
}