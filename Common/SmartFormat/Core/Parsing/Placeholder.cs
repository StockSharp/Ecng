﻿using System.Collections.Generic;
using System.Text;

namespace SmartFormat.Core.Parsing
{
	/// <summary>
	/// A placeholder is the part of a format string between the { braces }.
	/// </summary>
	/// <example>
	/// For example, in "{Items.Length,10:choose(1,2,3):one|two|three}",
	/// the <see cref="Alignment"/>s is "10", 
	/// the <see cref="Selector"/>s are "Items" and "Length", 
	/// the <see cref="FormatterName" /> is "choose",
	/// the <see cref="FormatterOptions"/> is "1,2,3",
	/// and the <see cref="Format"/> is "one|two|three".
	/// </example>
	public class Placeholder : FormatItem
	{
		public Placeholder(Format parent, int startIndex, int nestedDepth) : base(parent, startIndex)
		{
			this.parent = parent;
			this.Selectors = new List<Selector>();
			this.NestedDepth = nestedDepth;
			this.FormatterName = "";
			this.FormatterOptions = "";
		}

		public readonly Format parent;
		public int NestedDepth { get; set; }

		public List<Selector> Selectors { get; private set; }
		public int Alignment { get; set; }
		public string FormatterName { get; set; }
		public string FormatterOptions { get; set; }
		public Format Format { get; set; }

		public override string ToString()
		{
			var result = new StringBuilder(endIndex - startIndex);
			result.Append('{');
			foreach (var s in Selectors)
			{
				result.Append(s.baseString, s.operatorStart, s.endIndex - s.operatorStart);
			}
			if (Alignment != 0)
			{
				result.Append(',');
				result.Append(Alignment);
			}
			if (FormatterName != "")
			{
				result.Append(':');
				result.Append(FormatterName);
				if (FormatterOptions != "")
				{
					result.Append('(');
					result.Append(FormatterOptions);
					result.Append(')');
				}
			}
			if (Format != null)
			{
				result.Append(':');
				result.Append(Format.ToString());
			}
			result.Append('}');
			return result.ToString();
		}
	}
}
