namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Provides methods to parse and format date and time strings using a fast custom parser.
/// </summary>
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
		Mls,
		Mcs,
		Nano,
	}

	private readonly Tuple<Parts, string>[] _parts;

	private readonly int _yearStart;
	private readonly int _monthStart;
	private readonly int _dayStart;
	private readonly int _hourStart;
	private readonly int _minuteStart;
	private readonly int _secondStart;
	private readonly int _milliStart;
	private readonly int _microStart;
	private readonly int _nanoStart;

	private readonly int _timeZoneStart;

	private readonly bool _isYearTwoChars;
	private readonly bool _isMonthTwoChars = true;
	private readonly bool _isDayTwoChars = true;

	/// <summary>
	/// Gets the date and time format template used for parsing and formatting.
	/// </summary>
	public string Template { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FastDateTimeParser"/> class with the specified format template.
	/// </summary>
	/// <param name="template">The date and time format template.</param>
	/// <exception cref="ArgumentNullException">Thrown when the template is null or empty.</exception>
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

		if (_milliStart == -1)
			_milliStart = template.IndexOf('F');

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

		_microStart = -1;
		_nanoStart = -1;

		if (_milliStart != -1)
		{
			parts.Add(_milliStart, Parts.Mls);

			var microStart = _milliStart + 3;

			if (template.Length > (microStart + 1) && (template[microStart] == 'f' || template[microStart] == 'F'))
			{
				_microStart = microStart;
				parts.Add(_microStart, Parts.Mcs);

				var nanoStart = _microStart + 3;

				if (template.Length > (nanoStart + 1) && (template[nanoStart] == 'f' || template[nanoStart] == 'F'))
				{
					_nanoStart = nanoStart;
					parts.Add(_nanoStart, Parts.Nano);
				}
			}
		}

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

			prevIndex += part.Value switch
			{
				Parts.Year => _isYearTwoChars ? 2 : 4,
				Parts.Month => _isMonthTwoChars ? 2 : 1,
				Parts.Day => _isDayTwoChars ? 2 : 1,
				Parts.Mls => 3,
				Parts.Mcs => 3,
				Parts.Nano => 3,
				_ => 2,
			};
		}

		_parts = [.. parts2];

		//TimeHelper.InitBounds(template, 'y', out _yearStart, out _yearLen);
		//TimeHelper.InitBounds(template, 'M', out _monthStart, out _monthLen);
		//TimeHelper.InitBounds(template, 'd', out _dayStart, out _dayLen);
		//TimeHelper.InitBounds(template, 'H', out _hourStart, out _hourLen);
		//TimeHelper.InitBounds(template, 'm', out _minuteStart, out _minuteLen);
		//TimeHelper.InitBounds(template, 's', out _secondStart, out _secondLen);
		//TimeHelper.InitBounds(template, 'f', out _milliStart, out _milliLen);
	}

	/// <summary>
	/// Parses the specified input string into a <see cref="DateTime"/> based on the predefined template.
	/// </summary>
	/// <param name="input">The string input representing the date and time.</param>
	/// <returns>A <see cref="DateTime"/> object parsed from the input string.</returns>
	/// <exception cref="InvalidCastException">Thrown when the input cannot be parsed into a valid <see cref="DateTime"/>.</exception>
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
			var micro = _microStart == -1 ? 0 : (input[_microStart] - '0') * 100 + (input[_microStart + 1] - '0') * 10 + (input[_microStart + 2] - '0');
			var nano = _nanoStart == -1 ? 0 : (input[_nanoStart] - '0') * 100 + (input[_nanoStart + 1] - '0') * 10 + (input[_nanoStart + 2] - '0');

			var dt = new DateTime(years, months, days, hours, minutes, seconds, millis);

			if (micro > 0)
			{
				dt = dt.AddMicroseconds(micro);

				if (nano > 0)
					dt = dt.AddNanoseconds(nano);
			}

			return dt;
			//}
		}
		catch (Exception ex)
		{
			throw new InvalidCastException($"Cannot convert {input} with format {Template} to {typeof(DateTime).Name}.", ex);
		}
	}

	/// <summary>
	/// Parses the specified input string into a <see cref="DateTimeOffset"/> based on the predefined template,
	/// applying the time zone if specified.
	/// </summary>
	/// <param name="input">The string input representing the date and time with time zone.</param>
	/// <returns>A <see cref="DateTimeOffset"/> object parsed from the input string.</returns>
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

	/// <summary>
	/// Converts the specified <see cref="DateTime"/> to its string representation based on the predefined template.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value to format.</param>
	/// <returns>A string representation of the date and time.</returns>
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
				case Parts.Mcs:
					Append(builder, value.GetMicroseconds(), 3);
					break;
				case Parts.Nano:
					Append(builder, value.GetNanoseconds(), 3);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Appends the specified integer value to the <see cref="StringBuilder"/> with a given size.
	/// </summary>
	/// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
	/// <param name="value">The integer value to append.</param>
	/// <param name="size">The number of digits to include.</param>
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

	/// <inheritdoc />
	public override string ToString() => Template;
}