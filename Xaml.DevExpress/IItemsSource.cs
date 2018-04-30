namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	using DevExpress.Xpf.Editors.Settings;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// Provides an interface that is implemented by classes when a scenario calls for use of a collection of values represented by a ComboBox for a given property.
	/// </summary>
	public interface IItemsSource
	{
		/// <summary>
		/// Get the collection of values represented by a ComboBox.
		/// </summary>
		/// <returns>Collection of values represented by a ComboBox.</returns>
		IEnumerable<Tuple<string, object>> GetValues();
	}

	/// <summary>
	/// Represents an attribute that is set on a property to identify the IItemsSource-derived class that will be used.
	/// </summary>
	public class ItemsSourceAttribute : Attribute
	{
		/// <summary>
		/// Gets the type to use.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceAttribute"/>.
		/// </summary>
		/// <param name="type">The type to use.</param>
		public ItemsSourceAttribute(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (!typeof(IItemsSource).IsAssignableFrom(type))
				throw new ArgumentException("Type {0} must implement the {1} interface.".Translate().Put(type, nameof(IItemsSource)), nameof(type));

			Type = type;
		}
	}

	/// <summary>
	/// Contains settings specific to a combobox editor with uses <see cref="IItemsSource"/>.
	/// </summary>
	public class ItemsSourceEditSettings : ComboBoxEditSettings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceEditSettings"/>.
		/// </summary>
		public ItemsSourceEditSettings()
		{
			DisplayMember = "Item1";
			ValueMember = "Item2";
			ItemsSource = ComboBoxItems;
		}

		/// <summary>
		/// Items.
		/// </summary>
		public IList<Tuple<string, object>> ComboBoxItems { get; } = new ObservableCollection<Tuple<string, object>>();
	}
}