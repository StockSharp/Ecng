namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class FastTimeSpanParser
	{
		private enum Parts
		{
			String,
			Day,
			Hour,
			Minute,
			Second,
			Mls
		}

		private readonly Tuple<Parts, string>[] _parts;

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

		public string Template { get; }

		public FastTimeSpanParser(string template)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException(nameof(template));

			Template = template;

			template = template.Remove("\\");

			_dayStart = template.IndexOf('d');
			_hourStart = template.IndexOf('h');
			_minuteStart = template.IndexOf('m');
			_secondStart = template.IndexOf('s');
			_milliStart = template.IndexOf('f');

			if (_dayStart == -1)
				_dayStart = template.IndexOf('D');

			if (_hourStart == -1)
				_hourStart = template.IndexOf('H');

			if (_minuteStart == -1)
				_minuteStart = template.IndexOf('M');

			if (_secondStart == -1)
				_secondStart = template.IndexOf('S');

			if (_milliStart == -1)
				_milliStart = template.IndexOf('F');

			var parts = new SortedList<int, Parts>();

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

				if (part.Value == Parts.Mls)
					prevIndex += 3;
				else
					prevIndex += 2;
			}

			_parts = parts2.ToArray();

			//TimeHelper.InitBounds(template, 'd', out _dayStart, out _dayLen);
			//TimeHelper.InitBounds(template, 'h', out _hourStart, out _hourLen);
			//TimeHelper.InitBounds(template, 'm', out _minuteStart, out _minuteLen);
			//TimeHelper.InitBounds(template, 's', out _secondStart, out _secondLen);
			//TimeHelper.InitBounds(template, 'f', out _milliStart, out _milliLen);
		}

		public TimeSpan Parse(string input)
		{
			try
			{
				var days = _dayStart == -1 ? 0 : (input[_dayStart] - '0') * 10 + (input[_dayStart + 1] - '0');

				var hours = _hourStart == -1 ? 0 : (input[_hourStart] - '0') * 10 + (input[_hourStart + 1] - '0');
				var minutes = _minuteStart == -1 ? 0 : (input[_minuteStart] - '0') * 10 + (input[_minuteStart + 1] - '0');
				var seconds = _secondStart == -1 ? 0 : (input[_secondStart] - '0') * 10 + (input[_secondStart + 1] - '0');

				var millis = _milliStart == -1 ? 0 : (input[_milliStart] - '0') * 100 + (input[_milliStart + 1] - '0') * 10 + (input[_milliStart + 2] - '0');

				return new TimeSpan(days, hours, minutes, seconds, millis);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(input, Template, typeof(TimeSpan).Name), ex);
			}
		}

		public string ToString(TimeSpan value)
		{
			var builder = new StringBuilder();

			foreach (var part in _parts)
			{
				switch (part.Item1)
				{
					case Parts.String:
						builder.Append(part.Item2);
						break;
					case Parts.Day:
						Append(builder, value.Days);
						break;
					case Parts.Hour:
						Append(builder, value.Hours);
						break;
					case Parts.Minute:
						Append(builder, value.Minutes);
						break;
					case Parts.Second:
						Append(builder, value.Seconds);
						break;
					case Parts.Mls:
						Append(builder, value.Milliseconds, 3);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return builder.ToString();
		}

		private static void Append(StringBuilder builder, int value, int size = 2)
		{
			if (size == 2)
			{
				builder.Append((char)(value / 10 + '0'));
				builder.Append((char)(value % 10 + '0'));
			}
			else
			{
				builder.Append((char)(value / 100 + '0'));
				builder.Append((char)((value - (value / 100) * 100) / 10 + '0'));
				builder.Append((char)(value % 10 + '0'));
			}
		}
	}
}