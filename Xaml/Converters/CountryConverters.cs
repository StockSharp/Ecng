namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Media.Imaging;
	using System.Collections.Generic;
	using System.Windows.Data;

	using Ecng.Collections;
	using Ecng.Common;

	public sealed class CountryIdToFlagImageSourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var countryId = value as string;

			if (countryId.IsEmpty())
				return null;

			try
			{
				var path = "/Ecng.Xaml;component/Resources/Flags/{0}.png".Put(countryId);
				var uri = new Uri(path, UriKind.Relative);
				var resourceStream = Application.GetResourceStream(uri);
				if (resourceStream == null)
					return null;

				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.StreamSource = resourceStream.Stream;
				bitmap.EndInit();
				return bitmap;
			}
			catch
			{
				return null;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class CountryIdToNameConverter : IValueConverter
	{
		#region countries

		private static readonly Dictionary<string, string> _countries = new Dictionary<string, string>
		{
			{ "_abkhazia", "Abkhazia" },
			{ "AF", "Afghanistan" },
			{ "AX", "Aland Islands" },
			{ "AL", "Albania" },
			{ "DZ", "Algeria" },
			{ "AS", "American Samoa" },
			{ "AD", "Andorra" },
			{ "AO", "Angola" },
			{ "AI", "Anguilla" },
			{ "AQ", "Antarctica" },
			{ "AG", "Antigua and Barbuda" },
			{ "AR", "Argentina" },
			{ "AM", "Armenia" },
			{ "AW", "Aruba" },
			{ "AU", "Australia" },
			{ "AT", "Austria" },
			{ "AZ", "Azerbaijan" },
			{ "BS", "Bahamas" },
			{ "BH", "Bahrain" },
			{ "BD", "Bangladesh" },
			{ "BB", "Barbados" },
			{ "_basque-country", "Basque Country" },
			{ "BY", "Belarus" },
			{ "BE", "Belgium" },
			{ "BZ", "Belize" },
			{ "BJ", "Benin" },
			{ "BM", "Bermuda" },
			{ "BT", "Bhutan" },
			{ "BO", "Bolivia" },
			{ "BA", "Bosnia and Herzegovina" },
			{ "BW", "Botswana" },
			{ "BR", "Brazil" },
			{ "_british-antarctic-territory", "British Indian Ocean Territory" },
			{ "VG", "British Virgin Islands" },
			{ "BN", "Brunei Darussalam" },
			{ "BG", "Bulgaria" },
			{ "BF", "Burkina Faso" },
			{ "BI", "Burundi" },
			{ "KH", "Cambodia" },
			{ "CM", "Cameroon" },
			{ "CA", "Canada" },
			{ "CV", "Cape Verde" },
			{ "KY", "Cayman Islands" },
			{ "CF", "Central African Republic" },
			{ "TD", "Chad" },
			{ "CL", "Chile" },
			{ "CN", "China" },
			{ "CX", "Christmas Island" },
			{ "CC", "Cocos (Keeling) Islands" },
			{ "CO", "Colombia" },
			{ "_commonwealth", "Commonwealth" },
			{ "KM", "Comoros" },
			{ "CG", "Congo (Brazzaville)" },
			{ "CD", "Congo, Democratic Republic of the" },
			{ "CK", "Cook Islands" },
			{ "CR", "Costa Rica" },
			{ "CI", "Cote d'Ivoire" },
			{ "HR", "Croatia" },
			{ "CU", "Cuba" },
			{ "CW", "Curacao" },
			{ "CY", "Cyprus" },
			{ "CZ", "Czech Republic" },
			{ "DK", "Denmark" },
			{ "DJ", "Djibouti" },
			{ "DM", "Dominica" },
			{ "DO", "Dominican Republic" },
			{ "EC", "Ecuador" },
			{ "EG", "Egypt" },
			{ "SV", "El Salvador" },
			{ "GQ", "Equatorial Guinea" },
			{ "ER", "Eritrea" },
			{ "EE", "Estonia" },
			{ "ET", "Ethiopia" },
			{ "EU", "European Union" },
			{ "FK", "Falkland Islands (Malvinas)" },
			{ "FO", "Faroe Islands" },
			{ "FJ", "Fiji" },
			{ "FI", "Finland" },
			{ "FR", "France" },
			{ "PF", "French Polynesia" },
			{ "TF", "French Southern Territories" },
			{ "GA", "Gabon" },
			{ "GM", "Gambia" },
			{ "GE", "Georgia" },
			{ "DE", "Germany" },
			{ "GH", "Ghana" },
			{ "GI", "Gibraltar" },
			{ "_gosquared", "Gosquared" },
			{ "GR", "Greece" },
			{ "GL", "Greenland" },
			{ "GD", "Grenada" },
			{ "GU", "Guam" },
			{ "GT", "Guatemala" },
			{ "GG", "Guernsey" },
			{ "GN", "Guinea" },
			{ "GW", "Guinea-Bissau" },
			{ "GY", "Guyana" },
			{ "HT", "Haiti" },
			{ "VA", "Holy See (Vatican City State)" },
			{ "HN", "Honduras" },
			{ "HK", "Hong Kong, Special Administrative Region of China" },
			{ "HU", "Hungary" },
			{ "IS", "Iceland" },
			{ "IN", "India" },
			{ "ID", "Indonesia" },
			{ "IR", "Iran, Islamic Republic of" },
			{ "IQ", "Iraq" },
			{ "IE", "Ireland" },
			{ "IM", "Isle of Man" },
			{ "IL", "Israel" },
			{ "IT", "Italy" },
			{ "JM", "Jamaica" },
			{ "JP", "Japan" },
			{ "JE", "Jersey" },
			{ "JO", "Jordan" },
			{ "KZ", "Kazakhstan" },
			{ "KE", "Kenya" },
			{ "KI", "Kiribati" },
			{ "KP", "Korea, Democratic People's Republic of" },
			{ "KR", "Korea, Republic of" },
			{ "_kosovo", "Kosovo" },
			{ "KW", "Kuwait" },
			{ "KG", "Kyrgyzstan" },
			{ "LA", "Lao PDR" },
			{ "LV", "Latvia" },
			{ "LB", "Lebanon" },
			{ "LS", "Lesotho" },
			{ "LR", "Liberia" },
			{ "LY", "Libya" },
			{ "LI", "Liechtenstein" },
			{ "LT", "Lithuania" },
			{ "LU", "Luxembourg" },
			{ "MO", "Macao, Special Administrative Region of China" },
			{ "MK", "Macedonia, Republic of" },
			{ "MG", "Madagascar" },
			{ "MW", "Malawi" },
			{ "MY", "Malaysia" },
			{ "MV", "Maldives" },
			{ "ML", "Mali" },
			{ "MT", "Malta" },
			{ "_mars", "Mars" },
			{ "MH", "Marshall Islands" },
			{ "MQ", "Martinique" },
			{ "MR", "Mauritania" },
			{ "MU", "Mauritius" },
			{ "YT", "Mayotte" },
			{ "MX", "Mexico" },
			{ "FM", "Micronesia, Federated States of" },
			{ "MD", "Moldova" },
			{ "MC", "Monaco" },
			{ "MN", "Mongolia" },
			{ "ME", "Montenegro" },
			{ "MS", "Montserrat" },
			{ "MA", "Morocco" },
			{ "MZ", "Mozambique" },
			{ "MM", "Myanmar" },
			{ "_nagorno-karabakh", "Nagorno Karabakh" },
			{ "NA", "Namibia" },
			{ "NR", "Nauru" },
			{ "NP", "Nepal" },
			{ "NL", "Netherlands" },
			{ "AN", "Netherlands Antilles" },
			{ "NC", "New Caledonia" },
			{ "NZ", "New Zealand" },
			{ "NI", "Nicaragua" },
			{ "NE", "Niger" },
			{ "NG", "Nigeria" },
			{ "NU", "Niue" },
			{ "NF", "Norfolk Island" },
			{ "MP", "Northern Mariana Islands" },
			{ "NO", "Norway" },
			{ "_northern-cyprus", "Nothern Cyprus" },
			{ "OM", "Oman" },
			{ "PK", "Pakistan" },
			{ "PW", "Palau" },
			{ "PS", "Palestinian Territory, Occupied" },
			{ "PA", "Panama" },
			{ "PG", "Papua New Guinea" },
			{ "PY", "Paraguay" },
			{ "PE", "Peru" },
			{ "PH", "Philippines" },
			{ "PN", "Pitcairn" },
			{ "PL", "Poland" },
			{ "PT", "Portugal" },
			{ "PR", "Puerto Rico" },
			{ "QA", "Qatar" },
			{ "RO", "Romania" },
			{ "RU", "Russian Federation" },
			{ "RW", "Rwanda" },
			{ "SH", "Saint Helena" },
			{ "KN", "Saint Kitts and Nevis" },
			{ "LC", "Saint Lucia" },
			{ "VC", "Saint Vincent and Grenadines" },
			{ "BL", "Saint-Barthelemy" },
			{ "MF", "Saint-Martin (French part)" },
			{ "WS", "Samoa" },
			{ "SM", "San Marino" },
			{ "ST", "Sao Tome and Principe" },
			{ "SA", "Saudi Arabia" },
			{ "_scotland", "Scotland" },
			{ "SN", "Senegal" },
			{ "RS", "Serbia" },
			{ "SC", "Seychelles" },
			{ "SL", "Sierra Leone" },
			{ "SG", "Singapore" },
			{ "SK", "Slovakia" },
			{ "SI", "Slovenia" },
			{ "SB", "Solomon Islands" },
			{ "SO", "Somalia" },
			{ "_somaliland", "Somaliland" },
			{ "ZA", "South Africa" },
			{ "GS", "South Georgia and the South Sandwich Islands" },
			{ "_south-ossetia", "South Ossetia" },
			{ "SS", "South Sudan" },
			{ "ES", "Spain" },
			{ "LK", "Sri Lanka" },
			{ "SD", "Sudan" },
			{ "SR", "Suriname" },
			{ "SZ", "Swaziland" },
			{ "SE", "Sweden" },
			{ "CH", "Switzerland" },
			{ "SY", "Syrian Arab Republic (Syria)" },
			{ "TW", "Taiwan, Republic of China" },
			{ "TJ", "Tajikistan" },
			{ "TZ", "Tanzania, United Republic of" },
			{ "TH", "Thailand" },
			{ "TL", "Timor-Leste" },
			{ "TG", "Togo" },
			{ "TK", "Tokelau" },
			{ "TO", "Tonga" },
			{ "TT", "Trinidad and Tobago" },
			{ "TN", "Tunisia" },
			{ "TR", "Turkey" },
			{ "TM", "Turkmenistan" },
			{ "TC", "Turks and Caicos Islands" },
			{ "TV", "Tuvalu" },
			{ "UG", "Uganda" },
			{ "UA", "Ukraine" },
			{ "AE", "United Arab Emirates" },
			{ "GB", "United Kingdom" },
			{ "US", "United States of America" },
			{ "_unknown", "Unknown" },
			{ "UY", "Uruguay" },
			{ "UZ", "Uzbekistan" },
			{ "VU", "Vanuatu" },
			{ "VE", "Venezuela (Bolivarian Republic of)" },
			{ "VN", "Viet Nam" },
			{ "VI", "Virgin Islands, US" },
			{ "_wales", "Wales" },
			{ "WF", "Wallis and Futuna Islands" },
			{ "EH", "Western Sahara" },
			{ "YE", "Yemen" },
			{ "ZM", "Zambia" },
			{ "ZW", "Zimbabwe" },
		};

		#endregion

		public static IEnumerable<string> AllCountryIds
		{
			get { return _countries.Keys; }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var countryId = value as string;

			if (countryId.IsEmpty())
				return null;

			return _countries.TryGetValue(countryId);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
