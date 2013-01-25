namespace Ecng.Xaml.Grids
{
	using System.Windows.Data;

	class PropertyGroupDescriptionEx : PropertyGroupDescription
	{
		public PropertyGroupDescriptionEx(string propertyName, string header)
			: base(propertyName)
		{
			Header = header;
		}

		public PropertyGroupDescriptionEx(string propertyName, string header, IValueConverter converter)
			: base(propertyName, converter)
		{
			Header = header;
		}

		public string Header { get; set; }
	}
}