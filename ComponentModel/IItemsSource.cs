namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Reflection;

	public interface IItemsSource<TValue>
	{
		/// <summary>
		/// Collection of values represented by a ComboBox.
		/// </summary>
		IEnumerable<Tuple<string, TValue>> Values { get; }
	}

	/// <summary>
	/// Provides an interface that is implemented by classes when a scenario calls for use of a collection of values represented by a ComboBox for a given property.
	/// </summary>
	public interface IItemsSource : IItemsSource<object>
	{
	}

	/// <summary>
	/// Represents an attribute that is set on a property to identify the IItemsSource-derived class that will be used.
	/// </summary>
	public class ItemsSourceAttribute : Attribute
	{
		/// <summary>
		/// Gets the type to use.
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Allow user input.
		/// </summary>
		public bool IsEditable { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceAttribute"/>.
		/// </summary>
		/// <param name="type">The type to use.</param>
		public ItemsSourceAttribute(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (type.GetGenericType(typeof(IItemsSource<>)) == null)
				throw new ArgumentException("Type '{0}' must implement the '{1}' interface.".Translate().Put(type, typeof(IItemsSource<>)), nameof(type));

			Type = type;
		}
	}
}