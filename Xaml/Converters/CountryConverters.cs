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
			var code = CountryIdToNameConverter.ConvertFromObject(value);

			if (code == null)
				return null;

			try
			{
				var path = "/Ecng.Xaml;component/Resources/Flags/{0}.png".Put(code.Value);
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

		private static readonly Dictionary<CountryCodes, string> _countries = new Dictionary<CountryCodes, string>
		{
			{ CountryCodes.Abkhazia, "Abkhazia" },
			{ CountryCodes.AF, "Afghanistan" },
			{ CountryCodes.AX, "Aland Islands" },
			{ CountryCodes.AL, "Albania" },
			{ CountryCodes.DZ, "Algeria" },
			{ CountryCodes.AS, "American Samoa" },
			{ CountryCodes.AD, "Andorra" },
			{ CountryCodes.AO, "Angola" },
			{ CountryCodes.AI, "Anguilla" },
			{ CountryCodes.AQ, "Antarctica" },
			{ CountryCodes.AG, "Antigua and Barbuda" },
			{ CountryCodes.AR, "Argentina" },
			{ CountryCodes.AM, "Armenia" },
			{ CountryCodes.AW, "Aruba" },
			{ CountryCodes.AU, "Australia" },
			{ CountryCodes.AT, "Austria" },
			{ CountryCodes.AZ, "Azerbaijan" },
			{ CountryCodes.BS, "Bahamas" },
			{ CountryCodes.BH, "Bahrain" },
			{ CountryCodes.BD, "Bangladesh" },
			{ CountryCodes.BB, "Barbados" },
			{ CountryCodes.BasqueCountry, "Basque Country" },
			{ CountryCodes.BY, "Belarus" },
			{ CountryCodes.BE, "Belgium" },
			{ CountryCodes.BZ, "Belize" },
			{ CountryCodes.BJ, "Benin" },
			{ CountryCodes.BM, "Bermuda" },
			{ CountryCodes.BT, "Bhutan" },
			{ CountryCodes.BO, "Bolivia" },
			{ CountryCodes.BA, "Bosnia and Herzegovina" },
			{ CountryCodes.BW, "Botswana" },
			{ CountryCodes.BR, "Brazil" },
			{ CountryCodes.BritishAntarcticTerritory, "British Indian Ocean Territory" },
			{ CountryCodes.VG, "British Virgin Islands" },
			{ CountryCodes.BN, "Brunei Darussalam" },
			{ CountryCodes.BG, "Bulgaria" },
			{ CountryCodes.BF, "Burkina Faso" },
			{ CountryCodes.BI, "Burundi" },
			{ CountryCodes.KH, "Cambodia" },
			{ CountryCodes.CM, "Cameroon" },
			{ CountryCodes.CA, "Canada" },
			{ CountryCodes.CV, "Cape Verde" },
			{ CountryCodes.KY, "Cayman Islands" },
			{ CountryCodes.CF, "Central African Republic" },
			{ CountryCodes.TD, "Chad" },
			{ CountryCodes.CL, "Chile" },
			{ CountryCodes.CN, "China" },
			{ CountryCodes.CX, "Christmas Island" },
			{ CountryCodes.CC, "Cocos (Keeling) Islands" },
			{ CountryCodes.CO, "Colombia" },
			{ CountryCodes.Commonwealth, "Commonwealth" },
			{ CountryCodes.KM, "Comoros" },
			{ CountryCodes.CG, "Congo (Brazzaville)" },
			{ CountryCodes.CD, "Congo, Democratic Republic of the" },
			{ CountryCodes.CK, "Cook Islands" },
			{ CountryCodes.CR, "Costa Rica" },
			{ CountryCodes.CI, "Cote d'Ivoire" },
			{ CountryCodes.HR, "Croatia" },
			{ CountryCodes.CU, "Cuba" },
			{ CountryCodes.CW, "Curacao" },
			{ CountryCodes.CY, "Cyprus" },
			{ CountryCodes.CZ, "Czech Republic" },
			{ CountryCodes.DK, "Denmark" },
			{ CountryCodes.DJ, "Djibouti" },
			{ CountryCodes.DM, "Dominica" },
			{ CountryCodes.DO, "Dominican Republic" },
			{ CountryCodes.EC, "Ecuador" },
			{ CountryCodes.EG, "Egypt" },
			{ CountryCodes.SV, "El Salvador" },
			{ CountryCodes.GQ, "Equatorial Guinea" },
			{ CountryCodes.ER, "Eritrea" },
			{ CountryCodes.EE, "Estonia" },
			{ CountryCodes.ET, "Ethiopia" },
			{ CountryCodes.EU, "European Union" },
			{ CountryCodes.FK, "Falkland Islands (Malvinas)" },
			{ CountryCodes.FO, "Faroe Islands" },
			{ CountryCodes.FJ, "Fiji" },
			{ CountryCodes.FI, "Finland" },
			{ CountryCodes.FR, "France" },
			{ CountryCodes.PF, "French Polynesia" },
			{ CountryCodes.TF, "French Southern Territories" },
			{ CountryCodes.GA, "Gabon" },
			{ CountryCodes.GM, "Gambia" },
			{ CountryCodes.GE, "Georgia" },
			{ CountryCodes.DE, "Germany" },
			{ CountryCodes.GH, "Ghana" },
			{ CountryCodes.GI, "Gibraltar" },
			{ CountryCodes.Gosquared, "Gosquared" },
			{ CountryCodes.GR, "Greece" },
			{ CountryCodes.GL, "Greenland" },
			{ CountryCodes.GD, "Grenada" },
			{ CountryCodes.GU, "Guam" },
			{ CountryCodes.GT, "Guatemala" },
			{ CountryCodes.GG, "Guernsey" },
			{ CountryCodes.GN, "Guinea" },
			{ CountryCodes.GW, "Guinea-Bissau" },
			{ CountryCodes.GY, "Guyana" },
			{ CountryCodes.HT, "Haiti" },
			{ CountryCodes.VA, "Holy See (Vatican City State)" },
			{ CountryCodes.HN, "Honduras" },
			{ CountryCodes.HK, "Hong Kong, Special Administrative Region of China" },
			{ CountryCodes.HU, "Hungary" },
			{ CountryCodes.IS, "Iceland" },
			{ CountryCodes.IN, "India" },
			{ CountryCodes.ID, "Indonesia" },
			{ CountryCodes.IR, "Iran, Islamic Republic of" },
			{ CountryCodes.IQ, "Iraq" },
			{ CountryCodes.IE, "Ireland" },
			{ CountryCodes.IM, "Isle of Man" },
			{ CountryCodes.IL, "Israel" },
			{ CountryCodes.IT, "Italy" },
			{ CountryCodes.JM, "Jamaica" },
			{ CountryCodes.JP, "Japan" },
			{ CountryCodes.JE, "Jersey" },
			{ CountryCodes.JO, "Jordan" },
			{ CountryCodes.KZ, "Kazakhstan" },
			{ CountryCodes.KE, "Kenya" },
			{ CountryCodes.KI, "Kiribati" },
			{ CountryCodes.KP, "Korea, Democratic People's Republic of" },
			{ CountryCodes.KR, "Korea, Republic of" },
			{ CountryCodes.Kosovo, "Kosovo" },
			{ CountryCodes.KW, "Kuwait" },
			{ CountryCodes.KG, "Kyrgyzstan" },
			{ CountryCodes.LA, "Lao PDR" },
			{ CountryCodes.LV, "Latvia" },
			{ CountryCodes.LB, "Lebanon" },
			{ CountryCodes.LS, "Lesotho" },
			{ CountryCodes.LR, "Liberia" },
			{ CountryCodes.LY, "Libya" },
			{ CountryCodes.LI, "Liechtenstein" },
			{ CountryCodes.LT, "Lithuania" },
			{ CountryCodes.LU, "Luxembourg" },
			{ CountryCodes.MO, "Macao, Special Administrative Region of China" },
			{ CountryCodes.MK, "Macedonia, Republic of" },
			{ CountryCodes.MG, "Madagascar" },
			{ CountryCodes.MW, "Malawi" },
			{ CountryCodes.MY, "Malaysia" },
			{ CountryCodes.MV, "Maldives" },
			{ CountryCodes.ML, "Mali" },
			{ CountryCodes.MT, "Malta" },
			{ CountryCodes.Mars, "Mars" },
			{ CountryCodes.MH, "Marshall Islands" },
			{ CountryCodes.MQ, "Martinique" },
			{ CountryCodes.MR, "Mauritania" },
			{ CountryCodes.MU, "Mauritius" },
			{ CountryCodes.YT, "Mayotte" },
			{ CountryCodes.MX, "Mexico" },
			{ CountryCodes.FM, "Micronesia, Federated States of" },
			{ CountryCodes.MD, "Moldova" },
			{ CountryCodes.MC, "Monaco" },
			{ CountryCodes.MN, "Mongolia" },
			{ CountryCodes.ME, "Montenegro" },
			{ CountryCodes.MS, "Montserrat" },
			{ CountryCodes.MA, "Morocco" },
			{ CountryCodes.MZ, "Mozambique" },
			{ CountryCodes.MM, "Myanmar" },
			{ CountryCodes.NagornoKarabakh, "Nagorno Karabakh" },
			{ CountryCodes.NA, "Namibia" },
			{ CountryCodes.NR, "Nauru" },
			{ CountryCodes.NP, "Nepal" },
			{ CountryCodes.NL, "Netherlands" },
			{ CountryCodes.AN, "Netherlands Antilles" },
			{ CountryCodes.NC, "New Caledonia" },
			{ CountryCodes.NZ, "New Zealand" },
			{ CountryCodes.NI, "Nicaragua" },
			{ CountryCodes.NE, "Niger" },
			{ CountryCodes.NG, "Nigeria" },
			{ CountryCodes.NU, "Niue" },
			{ CountryCodes.NF, "Norfolk Island" },
			{ CountryCodes.MP, "Northern Mariana Islands" },
			{ CountryCodes.NO, "Norway" },
			{ CountryCodes.NorthernCyprus, "Nothern Cyprus" },
			{ CountryCodes.OM, "Oman" },
			{ CountryCodes.PK, "Pakistan" },
			{ CountryCodes.PW, "Palau" },
			{ CountryCodes.PS, "Palestinian Territory, Occupied" },
			{ CountryCodes.PA, "Panama" },
			{ CountryCodes.PG, "Papua New Guinea" },
			{ CountryCodes.PY, "Paraguay" },
			{ CountryCodes.PE, "Peru" },
			{ CountryCodes.PH, "Philippines" },
			{ CountryCodes.PN, "Pitcairn" },
			{ CountryCodes.PL, "Poland" },
			{ CountryCodes.PT, "Portugal" },
			{ CountryCodes.PR, "Puerto Rico" },
			{ CountryCodes.QA, "Qatar" },
			{ CountryCodes.RO, "Romania" },
			{ CountryCodes.RU, "Russian Federation" },
			{ CountryCodes.RW, "Rwanda" },
			{ CountryCodes.SH, "Saint Helena" },
			{ CountryCodes.KN, "Saint Kitts and Nevis" },
			{ CountryCodes.LC, "Saint Lucia" },
			{ CountryCodes.VC, "Saint Vincent and Grenadines" },
			{ CountryCodes.BL, "Saint-Barthelemy" },
			{ CountryCodes.MF, "Saint-Martin (French part)" },
			{ CountryCodes.WS, "Samoa" },
			{ CountryCodes.SM, "San Marino" },
			{ CountryCodes.ST, "Sao Tome and Principe" },
			{ CountryCodes.SA, "Saudi Arabia" },
			{ CountryCodes.Scotland, "Scotland" },
			{ CountryCodes.SN, "Senegal" },
			{ CountryCodes.RS, "Serbia" },
			{ CountryCodes.SC, "Seychelles" },
			{ CountryCodes.SL, "Sierra Leone" },
			{ CountryCodes.SG, "Singapore" },
			{ CountryCodes.SK, "Slovakia" },
			{ CountryCodes.SI, "Slovenia" },
			{ CountryCodes.SB, "Solomon Islands" },
			{ CountryCodes.SO, "Somalia" },
			{ CountryCodes.Somaliland, "Somaliland" },
			{ CountryCodes.ZA, "South Africa" },
			{ CountryCodes.GS, "South Georgia and the South Sandwich Islands" },
			{ CountryCodes.SouthOssetia, "South Ossetia" },
			{ CountryCodes.SS, "South Sudan" },
			{ CountryCodes.ES, "Spain" },
			{ CountryCodes.LK, "Sri Lanka" },
			{ CountryCodes.SD, "Sudan" },
			{ CountryCodes.SR, "Suriname" },
			{ CountryCodes.SZ, "Swaziland" },
			{ CountryCodes.SE, "Sweden" },
			{ CountryCodes.CH, "Switzerland" },
			{ CountryCodes.SY, "Syrian Arab Republic (Syria)" },
			{ CountryCodes.TW, "Taiwan, Republic of China" },
			{ CountryCodes.TJ, "Tajikistan" },
			{ CountryCodes.TZ, "Tanzania, United Republic of" },
			{ CountryCodes.TH, "Thailand" },
			{ CountryCodes.TL, "Timor-Leste" },
			{ CountryCodes.TG, "Togo" },
			{ CountryCodes.TK, "Tokelau" },
			{ CountryCodes.TO, "Tonga" },
			{ CountryCodes.TT, "Trinidad and Tobago" },
			{ CountryCodes.TN, "Tunisia" },
			{ CountryCodes.TR, "Turkey" },
			{ CountryCodes.TM, "Turkmenistan" },
			{ CountryCodes.TC, "Turks and Caicos Islands" },
			{ CountryCodes.TV, "Tuvalu" },
			{ CountryCodes.UG, "Uganda" },
			{ CountryCodes.UA, "Ukraine" },
			{ CountryCodes.AE, "United Arab Emirates" },
			{ CountryCodes.GB, "United Kingdom" },
			{ CountryCodes.US, "United States of America" },
			{ CountryCodes.UY, "Uruguay" },
			{ CountryCodes.UZ, "Uzbekistan" },
			{ CountryCodes.VU, "Vanuatu" },
			{ CountryCodes.VE, "Venezuela (Bolivarian Republic of)" },
			{ CountryCodes.VN, "Viet Nam" },
			{ CountryCodes.VI, "Virgin Islands, US" },
			{ CountryCodes.Wales, "Wales" },
			{ CountryCodes.WF, "Wallis and Futuna Islands" },
			{ CountryCodes.EH, "Western Sahara" },
			{ CountryCodes.YE, "Yemen" },
			{ CountryCodes.ZM, "Zambia" },
			{ CountryCodes.ZW, "Zimbabwe" },
		};

		#endregion

		public static CountryCodes? ConvertFromObject(object value)
		{
			if (value == null)
				return null;

			CountryCodes code;

			var strCode = value as string;
			if (strCode != null)
				return Enum.TryParse(strCode, true, out code) ? code : (CountryCodes?)null;

			if (value is CountryCodes)
				return (CountryCodes) value;

			return null;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var code = ConvertFromObject(value);

			return code == null ? null : _countries.TryGetValue(code.Value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
