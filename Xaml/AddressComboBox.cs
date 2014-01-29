namespace Ecng.Xaml
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Net;
	using System.Windows.Controls;

	using Ecng.Common;

	/// <summary>
	/// Выпадающий список для выбора адреса сервера.
	/// </summary>
	public class AddressComboBox : ComboBox
	{
		/// <summary>
		/// Элемент списка <see cref="AddressComboBox"/>.
		/// </summary>
		public class ComboItem
		{
			internal ComboItem(EndPoint address, string title)
			{
				if (address == null)
					throw new ArgumentNullException("address");

				if (title.IsEmpty())
					throw new ArgumentNullException("title");

				Address = address;
				Title = title + " ({0})".Put(address.To<string>());
			}

			/// <summary>
			/// Адрес.
			/// </summary>
			public EndPoint Address { get; private set; }

			/// <summary>
			/// Отображаемое имя.
			/// </summary>
			public string Title { get; private set; }
		}

		private readonly ObservableCollection<ComboItem> _items = new ObservableCollection<ComboItem>();

		/// <summary>
		/// Создать <see cref="AddressComboBox"/>.
		/// </summary>
		public AddressComboBox()
		{
			DisplayMemberPath = "Title";
			Width = 170;

			ItemsSource = _items;
		}

		/// <summary>
		/// Добавить адрес в список.
		/// </summary>
		/// <param name="address">Адрес.</param>
		/// <param name="title">Отображаемое имя.</param>
		public void AddAddress(EndPoint address, string title)
		{
			_items.Add(new ComboItem(address, title));
		}

		/// <summary>
		/// Выбранный адрес сервера.
		/// </summary>
		public EndPoint SelectedAddress
		{
			get { return SelectedIndex == -1 ? null : ((ComboItem)SelectedItem).Address; }
			set
			{
				if (value == null)
					SelectedIndex = -1;
				else
				{
					var item = _items.FirstOrDefault(i => i.Address.Equals(value));

					if (item == null)
					{
						item = new ComboItem(value, value.ToString());
						_items.Add(item);
					}

					SelectedItem = item;
				}
			}
		}
	}
}