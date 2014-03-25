namespace Ecng.Xaml
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Controls;

	using Ecng.Common;

	/// <summary>
	/// Выпадающий список для выбора адреса сервера.
	/// </summary>
	public class AddressComboBox<TAddress> : ComboBox
		where TAddress : class
	{
		/// <summary>
		/// Элемент списка <see cref="AddressComboBox{TAddress}"/>.
		/// </summary>
		public class ComboItem
		{
			internal ComboItem(TAddress address, string title)
			{
				if (address == null)
					throw new ArgumentNullException("address");

				//if (title.IsEmpty())
				//	throw new ArgumentNullException("title");

				Address = address;

				Title = address.To<string>();

				if (!title.IsEmpty())
					Title = title + " ({0})".Put(Title);
			}

			/// <summary>
			/// Адрес.
			/// </summary>
			public TAddress Address { get; private set; }

			/// <summary>
			/// Отображаемое имя.
			/// </summary>
			public string Title { get; private set; }
		}

		private readonly ObservableCollection<ComboItem> _items = new ObservableCollection<ComboItem>();

		/// <summary>
		/// Создать <see cref="AddressComboBox{TAddress}"/>.
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
		public void AddAddress(TAddress address, string title)
		{
			_items.Add(new ComboItem(address, title));
		}

		/// <summary>
		/// Выбранный адрес сервера.
		/// </summary>
		public TAddress SelectedAddress
		{
			get
			{
				if (SelectedIndex != -1)
					return ((ComboItem)SelectedItem).Address;
				else
				{
					if (Text.IsEmpty())
						return null;

					try
					{
						var addr = Text.To<TAddress>();
						_items.Add(new ComboItem(addr, string.Empty));
						return addr;
					}
					catch (Exception)
					{
						return null;
					}
				}
			}
			set
			{
				if (value == null)
					SelectedIndex = -1;
				else
				{
					var item = _items.FirstOrDefault(i => i.Address.Equals(value));

					if (item == null)
					{
						item = new ComboItem(value, value.To<string>());
						_items.Add(item);
					}

					SelectedItem = item;
				}
			}
		}
	}
}