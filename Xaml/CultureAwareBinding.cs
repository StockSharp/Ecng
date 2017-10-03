namespace Ecng.Xaml
{
	using System.Globalization;
	using System.Windows.Data;

	// http://stackoverflow.com/a/14163432/1086121
	public class CultureAwareBinding : Binding
	{
		public CultureAwareBinding()
		{
			ConverterCulture = CultureInfo.CurrentCulture;
		}

		public CultureAwareBinding(string path)
			: base(path)
		{
			ConverterCulture = CultureInfo.CurrentCulture;
		}
	}
}