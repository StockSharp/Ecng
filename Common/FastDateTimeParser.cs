namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class FastDateTimeParser
	{
		private enum Parts
		{
			String,
			Year,
			Month,
			Day,
			Hour,
			Minute,
			Second,
			Mls
		}

		private readonly Tuple<Parts, string>[] _parts;

		private readonly int _yearStart;
		//private readonly int _yearLen;

		private readonly int _monthStart;
		//private readonly int _monthLen;

		private readonly int _dayStart;
		//private readonly int _dayLen;

		private readonly int _hourStart;
		//private readonly int _hourLen;

		private readonly int _minuteStart;
		//private readonly int _minuteLen;

		private readonly int _secondStart;
		//private readonly int _secondLen;

		private readonly int _milliStart;
		//private readonly int _milliLen;

		private readonly int _timeZoneStart;

		private readonly bool _isYearTwoChars;
		private readonly bool _isMonthTwoChars = true;
		private readonly bool _isDayTwoChars = true;

		public string Template { get; }
	
		public FastDateTimeParser(string template)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException(nameof(template));

			Template = template;

			_yearStart = template.IndexOf('y');
			_monthStart = template.IndexOf('M');
			_dayStart = template.IndexOf('d');
			_hourStart = template.IndexOf('H');
			_minuteStart = template.IndexOf('m');
			_secondStart = template.IndexOf('s');
			_milliStart = template.IndexOf('f');
			_timeZoneStart = template.IndexOf('z');

			if (_yearStart == -1)
				_yearStart = template.IndexOf('Y');

			if (_dayStart == -1)
				_dayStart = template.IndexOf('D');

			if (_monthStart == -1 && _minuteStart != -1 && _hourStart == -1)
			{
				_monthStart = _minuteStart;
				_minuteStart = -1;
			}

			if (_yearStart != -1)
				_isYearTwoChars = template.Length < _yearStart + 3 || template[_yearStart + 2] != template[_yearStart];

			if (_monthStart != -1)
				_isMonthTwoChars = template.Length > _monthStart + 1 && template[_monthStart + 1] == template[_monthStart];

			if (_dayStart != -1)
				_isDayTwoChars = template.Length > _dayStart + 1 && template[_dayStart + 1] == template[_dayStart];

			var parts = new SortedList<int, Parts>();

			if (_yearStart != -1)
				parts.Add(_yearStart, Parts.Year);

			if (_monthStart != -1)
				parts.Add(_monthStart, Parts.Month);

			if (_dayStart != -1)
				parts.Add(_dayStart, Parts.Day);

			if (_hourStart != -1)
				parts.Add(_hourStart, Parts.Hour);

			if (_minuteStart != -1)
				parts.Add(_minuteStart, Parts.Minute);

			if (_secondStart != -1)
				parts.Add(_secondStart, Parts.Second);

			if (_milliStart != -1)
				parts.Add(_milliStart, Parts.Mls);

			var parts2 = new List<Tuple<Parts, string>>();

			var prevIndex = 0;

			foreach (var part in parts)
			{
				if (prevIndex != part.Key)
				{
					var len = part.Key - prevIndex;
					parts2.Add(Tuple.Create(Parts.String, template.Substring(prevIndex, len)));
					prevIndex += len;
				}

				parts2.Add(Tuple.Create(part.Value, (string)null));

				if (part.Value == Parts.Year)
					prevIndex += _isYearTwoChars ? 2 : 4;
				else if (part.Value == Parts.Month)
					prevIndex += _isMonthTwoChars ? 2 : 1;
				else if (part.Value == Parts.Day)
					prevIndex += _isDayTwoChars ? 2 : 1;
				else if (part.Value == Parts.Mls)
					prevIndex += 3;
				else
					prevIndex += 2;
			}

			_parts = parts2.ToArray();

			//TimeHelper.InitBounds(template, 'y', out _yearStart, out _yearLen);
			//TimeHelper.InitBounds(template, 'M', out _monthStart, out _monthLen);
			//TimeHelper.InitBounds(template, 'd', out _dayStart, out _dayLen);
			//TimeHelper.InitBounds(template, 'H', out _hourStart, out _hourLen);
			//TimeHelper.InitBounds(template, 'm', out _minuteStart, out _minuteLen);
			//TimeHelper.InitBounds(template, 's', out _secondStart, out _secondLen);
			//TimeHelper.InitBounds(template, 'f', out _milliStart, out _milliLen);
		}

		public DateTime Parse(string input)
		{
			try
			{
				//fixed (char* stringBuffer = input)
				//{
				var years = _yearStart == -1 ? DateTime.Now.Year : (_isYearTwoChars ? ((DateTime.Now.Year / 1000) * 1000 + (input[_yearStart] - '0') * 10 + (input[_yearStart + 1] - '0')) : (input[_yearStart] - '0') * 1000 + (input[_yearStart + 1] - '0') * 100 + (input[_yearStart + 2] - '0') * 10 + (input[_yearStart + 3] - '0'));
				var months = _monthStart == -1 ? DateTime.Now.Month : (_isMonthTwoChars ? (input[_monthStart] - '0') * 10 + (input[_monthStart + 1] - '0') : input[_monthStart] - '0');
				var days = _dayStart == -1 ? DateTime.Now.Day : (_isDayTwoChars ? (input[_dayStart] - '0') * 10 + (input[_dayStart + 1] - '0') : input[_dayStart] - '0');

				var hours = _hourStart == -1 ? 0 : (input[_hourStart] - '0') * 10 + (input[_hourStart + 1] - '0');
				var minutes = _minuteStart == -1 ? 0 : (input[_minuteStart] - '0') * 10 + (input[_minuteStart + 1] - '0');
				var seconds = _secondStart == -1 ? 0 : (input[_secondStart] - '0') * 10 + (input[_secondStart + 1] - '0');

				var millis = _milliStart == -1 ? 0 : (input[_milliStart] - '0') * 100 + (input[_milliStart + 1] - '0') * 10 + (input[_milliStart + 2] - '0');

				return new DateTime(years, months, days, hours, minutes, seconds, millis);
				//}
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(input, Template, typeof(DateTime).Name), ex);
			}
		}

		public DateTimeOffset ParseDto(string input)
		{
			var dt = Parse(input);

			var timeZone = TimeSpan.Zero;

			if (_timeZoneStart != -1)
			{
				var pos = input[_timeZoneStart] == '+';
				var hours = (input[_timeZoneStart + 1] - '0') * 10 + (input[_timeZoneStart + 2] - '0');
				var minutes = (input[_timeZoneStart + 4] - '0') * 10 + (input[_timeZoneStart + 5] - '0');

				timeZone = new TimeSpan(pos ? hours : -hours, minutes, 0);
			}

			return dt.ApplyTimeZone(timeZone);
		}

		public string ToString(DateTime value)
		{
			var builder = new StringBuilder();

			foreach (var part in _parts)
			{
				switch (part.Item1)
				{
					case Parts.String:
						builder.Append(part.Item2);
						break;
					case Parts.Year:
						Append(builder, _isYearTwoChars ? value.Year % 100 : value.Year, _isYearTwoChars ? 2 : 4);
						break;
					case Parts.Month:
						Append(builder, value.Month, _isMonthTwoChars ? 2 : 1);
						break;
					case Parts.Day:
						Append(builder, value.Day, _isDayTwoChars ? 2 : 1);
						break;
					case Parts.Hour:
						Append(builder, value.Hour);
						break;
					case Parts.Minute:
						Append(builder, value.Minute);
						break;
					case Parts.Second:
						Append(builder, value.Second);
						break;
					case Parts.Mls:
						Append(builder, value.Millisecond, 3);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return builder.ToString();
		}

		private static void Append(StringBuilder builder, int value, int size = 2)
		{
			if (size == 1)
			{
				builder.Append((char)(value + '0'));
			}
			else if (size == 2)
			{
				builder.Append((char)(value / 10 + '0'));
				builder.Append((char)(value % 10 + '0'));
			}
			else if (size == 3)
			{
				builder.Append((char)(value / 100 + '0'));
				builder.Append((char)((value - (value / 100) * 100) / 10 + '0'));
				builder.Append((char)(value % 10 + '0'));
			}
			else
			{
				builder.Append((char)(value / 1000 + '0'));
				value -= (value / 1000) * 1000;
				builder.Append((char)(value / 100 + '0'));
				value -= (value / 100) * 100;
				builder.Append((char)(value / 10 + '0'));
				builder.Append((char)(value % 10 + '0'));
			}
		}
	}
}