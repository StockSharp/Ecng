﻿using System;
using System.Collections.Generic;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Parsing;

namespace SmartFormat.Extensions
{
	public class ChooseFormatter : IFormatter
	{
		private string[] names = { "choose", "c" };
		public string[] Names { get { return names; } set { names = value; } }

		private char splitChar = '|';
		public char SplitChar { get { return splitChar;  } set { splitChar = value; } }

		public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
		{
			if (formattingInfo.FormatterOptions == "") return false;
			var chooseOptions = formattingInfo.FormatterOptions.Split(splitChar);
			var formats = formattingInfo.Format.Split(splitChar);
			if (formats.Count < 2) return false;
			
			var chosenFormat = DetermineChosenFormat(formattingInfo, formats, chooseOptions);
			
			formattingInfo.Write(chosenFormat, formattingInfo.CurrentValue);
			
			return true;
		}

		private static Format DetermineChosenFormat(IFormattingInfo formattingInfo, IList<Format> choiceFormats, string[] chooseOptions)
		{
			var currentValue = formattingInfo.CurrentValue;
			var currentValueString = (currentValue == null) ? "null" : currentValue.ToString();

			var chosenIndex = Array.IndexOf(chooseOptions, currentValueString);
			
			// Validate the number of formats:
			if (choiceFormats.Count < chooseOptions.Length)
			{
				throw formattingInfo.FormattingException("You must specify at least " + chooseOptions.Length + " choices");
			}
			else if (choiceFormats.Count > chooseOptions.Length + 1)
			{
				throw formattingInfo.FormattingException("You cannot specify more than " + (chooseOptions.Length + 1) + " choices");
			}
			else if (chosenIndex == -1 && choiceFormats.Count == chooseOptions.Length)
			{
				throw formattingInfo.FormattingException("\"" + currentValueString + "\" is not a valid choice, and a \"default\" choice was not supplied");
			}

			if (chosenIndex == -1) {
				chosenIndex = choiceFormats.Count - 1;
			}

			var chosenFormat = choiceFormats[chosenIndex];
			return chosenFormat;
		}

	}
}