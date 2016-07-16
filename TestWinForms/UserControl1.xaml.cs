namespace TestWinForms
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Windows;
	using System.Xml.Serialization;

	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.Xaml.DevExp;

	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class UserControl1
	{
		/// <summary>
		/// Направления заявки.
		/// </summary>
		[System.Runtime.Serialization.DataContract]
		[Serializable]
		public enum OrderDirections
		{
			/// <summary>
			/// Заявка на покупку.
			/// </summary>
			[EnumMember]
			[EnumDisplayName("Покупка")]
			Buy,

			/// <summary>
			/// Заявка на продажу.
			/// </summary>
			[EnumMember]
			[EnumDisplayName("Продажа")]
			Sell,
		}

		private class Security
		{
			public string Id { get; set; } 
		}

		private class Trade
		{
			[DataMember]
			[DisplayName("Идентификатор")]
			[Description("Идентификатор сделки.")]
			[Identity]
			public long Id { get; set; }

			/// <summary>
			/// Инструмент, по которому была совершена сделка.
			/// </summary>
			[RelationSingle(IdentityType = typeof(string))]
			[XmlIgnore]
			[Browsable(false)]
			public Security Security { get; set; }

			/// <summary>
			/// Время совершения сделки.
			/// </summary>
			[DataMember]
			[DisplayName("Время")]
			[Description("Время совершения сделки.")]
			public DateTime Time { get; set; }

			/// <summary>
			/// Задержка в получении информации о сделке.
			/// </summary>
			[DataMember]
			[DisplayName("Задержка")]
			[Description("Задержка в получении информации о сделке.")]
			[Ignore]
			public TimeSpan Latency { get; set; }

			/// <summary>
			/// Количество контрактов в сделке.
			/// </summary>
			[DataMember]
			[DisplayName("Объем")]
			[Description("Количество контрактов в сделке.")]
			public decimal Volume { get; set; }

			/// <summary>
			/// Цена сделки.
			/// </summary>
			[DataMember]
			[DisplayName("Цена")]
			[Description("Цена сделки.")]
			public decimal Price { get; set; }

			/// <summary>
			/// Направление заявки (покупка или продажа), которая привела к сделке.
			/// </summary>
			[DataMember]
			[Nullable]
			[DisplayName("Направление")]
			[Description("Направление заявки (покупка или продажа), которая привела к сделке.")]
			public OrderDirections? OrderDirection { get; set; }
		}

		public UserControl1()
		{
			InitializeComponent();

			//TradesGrid.GroupingColumns.Add(TradesGrid.Columns[0]);

			var a = new[]
			{
				new Trade
				{
					Id = 1000,
					Price = 100.555m,
					Volume = 44,
					Security = new Security { Id = "SBER" },
					OrderDirection = OrderDirections.Buy,
					Time = DateTime.Now,
				},
				new Trade
				{
					Id = 6666,
					Price = 60.555m,
					Volume = 2,
					Security = new Security { Id = "LKOH" },
					Time = DateTime.Now,
				},
				new Trade
				{
					Id = 1000,
					Price = 100.555m,
					Volume = 44,
					Security = new Security { Id = "SBER" },
					OrderDirection = OrderDirections.Buy,
					Time = DateTime.Now,
				},
				new Trade
				{
					Id = 6666,
					Price = 60.555m,
					Volume = 2,
					Security = new Security { Id = "LKOH" },
					Time = DateTime.Now,
				},
			};
			//TradesGrid.ItemsSource = a;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//new Window1().ShowModal(this.GetWindow());
		}

		private void Excel_OnClick(object sender, RoutedEventArgs e)
		{
			new MessageBoxBuilder().Handler(new DevExpMessageBoxHandler()).Owner(this).Text("Test").Show();
			//new AboutWindow(this.GetWindow()).ShowModal(this);
		}

		private void Clipboard_OnClick(object sender, RoutedEventArgs e)
		{
			
		}

		private void Csv_OnClick(object sender, RoutedEventArgs e)
		{
			
		}

		private void ReLoad_OnClick(object sender, RoutedEventArgs e)
		{
			//TradesGrid.Load(TradesGrid.Save());
		}

		private void TradesGrid_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Changes.Items.Add(e.PropertyName);
		}
	}
}
