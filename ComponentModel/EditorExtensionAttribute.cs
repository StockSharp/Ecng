﻿namespace Ecng.ComponentModel
{
	using System;

	[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property)]
	public class EditorExtensionAttribute : Attribute
	{
		public bool AutoComplete { get; set; }

		public bool Sorted { get; set; }

		public bool IncludeObsolete { get; set; }

		public bool ShowSelectedItemsCount { get; set; }
	}
}