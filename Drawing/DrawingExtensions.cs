namespace Ecng.Drawing;

#if NETSTANDARD2_0
using System;
using Ecng.Common;
#endif
using System.Drawing;

/// <summary>
/// Provides extension methods for converting between integer, HTML color string representations and <see cref="Color"/>.
/// </summary>
public static class DrawingExtensions
{
	/// <summary>
	/// Creates a Color from the specified ARGB integer.
	/// </summary>
	/// <param name="argb">An integer representing the ARGB value.</param>
	/// <returns>A <see cref="Color"/> corresponding to the ARGB value specified.</returns>
	public static Color ToColor(this int argb)
		=> Color.FromArgb(argb);

	/// <summary>
	/// Converts an HTML color representation to a <see cref="Color"/>.
	/// </summary>
	/// <param name="htmlColor">
	/// A string representing the HTML color. Accepts formats such as "#RRGGBB", "#RGB" or the special case "LightGrey".
	/// </param>
	/// <returns>A <see cref="Color"/> corresponding to the HTML color provided.</returns>
	public static Color ToColor(this string htmlColor)
	{
#if NETSTANDARD2_0
		Color c = Color.Empty;

		// empty color
		if (htmlColor.IsEmpty())
			return c;

		// #RRGGBB or #RGB
		if ((htmlColor[0] == '#') && ((htmlColor.Length == 7) || (htmlColor.Length == 4)))
		{
			if (htmlColor.Length == 7)
			{
				c = Color.FromArgb(Convert.ToInt32(htmlColor.Substring(1, 2), 16),
								   Convert.ToInt32(htmlColor.Substring(3, 2), 16),
								   Convert.ToInt32(htmlColor.Substring(5, 2), 16));
			}
			else
			{
				string r = char.ToString(htmlColor[1]);
				string g = char.ToString(htmlColor[2]);
				string b = char.ToString(htmlColor[3]);

				c = Color.FromArgb(Convert.ToInt32(r + r, 16),
								   Convert.ToInt32(g + g, 16),
								   Convert.ToInt32(b + b, 16));
			}
		}

		// special case. Html requires LightGrey, but .NET uses LightGray
		if (c.IsEmpty && string.Equals(htmlColor, "LightGrey", StringComparison.OrdinalIgnoreCase))
		{
			c = Color.LightGray;
		}

		//// System color
		//if (c.IsEmpty)
		//{
		//	if (s_htmlSysColorTable == null)
		//	{
		//		InitializeHtmlSysColorTable();
		//	}

		//	s_htmlSysColorTable!.TryGetValue(htmlColor.ToLowerInvariant(), out c);
		//}

		//// resort to type converter which will handle named colors
		//if (c.IsEmpty)
		//{
		//	try
		//	{
		//		c = ColorConverterCommon.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
		//	}
		//	catch (Exception ex)
		//	{
		//		throw new ArgumentException(ex.Message, nameof(htmlColor), ex);
		//	}
		//}

		return c;
#else
		return ColorTranslator.FromHtml(htmlColor);
#endif
	}

	/// <summary>
	/// Converts a <see cref="Color"/> to its HTML representation.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>A string representing the HTML color value. Returns an empty string if the color is empty.</returns>
	public static string ToHtml(this Color color)
	{
#if NETSTANDARD2_0
		if (color.IsEmpty)
			return string.Empty;

		if (color.IsNamedColor)
		{
			// Special case for LightGray, which is not supported by Html
			if (color == Color.LightGray)
				return "LightGrey";

			return color.Name;
		}

		if (color.A < 255)
			return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
#else
		return ColorTranslator.ToHtml(color);
#endif
	}
}