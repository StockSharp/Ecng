﻿using System;
using SmartFormat.Core.Output;
using SmartFormat.Core.Parsing;
using SmartFormat.Core.Settings;

namespace SmartFormat.Core.Formatting
{
	/// <summary>
	/// Contains extra information about the item currently being formatted.
	/// These objects are not often used, so they are all wrapped up here.
	/// </summary>
	public class FormatDetails
	{
		public FormatDetails(SmartFormatter formatter, Format originalFormat, object[] originalArgs, FormatCache formatCache, IFormatProvider provider, IOutput output)
		{
			Formatter = formatter;
			OriginalFormat = originalFormat;
			OriginalArgs = originalArgs;
			FormatCache = formatCache;
			Provider = provider;
			Output = output;
		}

		/// <summary>
		/// The original formatter responsible for formatting this item.
		/// It can be used for evaluating nested formats.
		/// </summary>
		public SmartFormatter Formatter { get; private set; }

		public Format OriginalFormat { get; private set; }

		/// <summary>
		/// The original set of arguments passed to the format function.
		/// These provide global-access to the original arguments.
		/// </summary>
		public object[] OriginalArgs { get; private set; }
		/// <summary>
		/// This object can be used to cache resources between formatting calls.
		/// It will be null unless FormatWithCache is called.
		/// </summary>
		public FormatCache FormatCache { get; private set; }

		/// <summary>
		/// The Format Provider that can be used to determine how to
		/// format items such as numbers, dates, and anything else that
		/// might be culture-specific.
		/// </summary>
		public IFormatProvider Provider { get; private set; }

		public IOutput Output { get; private set; }

		/// <summary>
		/// If ErrorAction is set to OutputErrorsInResult, this will 
		/// contain the exception that caused the formatting error.
		/// </summary>
		public FormattingException FormattingException { get; set; }

		/// <summary>
		/// Contains case-sensitivity settings
		/// </summary>
		public SmartSettings Settings { get { return this.Formatter.Settings; } }
	}
}
