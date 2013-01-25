namespace Ecng.Configuration
{
	using System.ComponentModel;
	using System.Configuration;
	using System.Globalization;
	using System.Linq;

	public class ConnectionSettingsTypeConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var connectionStringName = (string)value;
			return ConfigManager
				.GetSection<ConnectionStringsSection>()
				.ConnectionStrings
				.Cast<ConnectionStringSettings>()
				.Single(settings => settings.Name == connectionStringName)
				.ConnectionString;
		}
	}
}