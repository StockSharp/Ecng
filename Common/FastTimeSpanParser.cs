namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Provides high-performance parsing and formatting of TimeSpan values based on a specified template.
/// </summary>
public class FastTimeSpanParser
{
	private enum Parts
	{
		String,
		Day,
		Hour,
		Minute,
		Second,
		Mls,
		Mcs,
		Tick,
		Nano,
	}

	private readonly (Parts part, string format)[] _parts;

	private readonly int _dayStart;
	private readonly int _hourStart;
	private readonly int _minuteStart;
	private readonly int _secondStart;
	private readonly int _milliStart;
	private readonly int _microStart;
	private readonly int _tickStart;
	private readonly int _nanoStart;

	/// <summary>
	/// Gets the template string used for parsing and formatting TimeSpan values.
	/// </summary>
	public string Template { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FastTimeSpanParser"/> class using the specified template.
	/// </summary>
	/// <param name="template">A format template that defines how TimeSpan values are parsed and formatted. Must not be null or empty.</param>
	/// <exception cref="ArgumentNullException">Thrown when the provided template is null or empty.</exception>
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

		_microStart = -1;
		_tickStart = -1;
		_nanoStart = -1;

		if (_milliStart != -1)
		{
			parts.Add(_milliStart, Parts.Mls);

			var microStart = _milliStart + 3;

			bool isValid(int idx)
				=> template[idx] == 'f' || template[idx] == 'F';

			if (template.Length > (microStart + 2) && isValid(microStart))
			{
				_microStart = microStart;
				parts.Add(_microStart, Parts.Mcs);

				var ticksStart = _microStart + 3;

				if (template.Length > ticksStart && isValid(ticksStart))
				{
					_tickStart = ticksStart;
					parts.Add(_tickStart, Parts.Tick);

					var nanoStart = _tickStart + 1;

					if (template.Length > (nanoStart + 1) && isValid(nanoStart))
					{
						_nanoStart = nanoStart;
						parts.Add(_nanoStart, Parts.Nano);
					}
				}
			}
		}

		var parts2 = new List<(Parts, string)>();

		var prevIndex = 0;

		foreach (var part in parts)
		{
			if (prevIndex != part.Key)
			{
				var len = part.Key - prevIndex;
				parts2.Add((Parts.String, template.Substring(prevIndex, len)));
				prevIndex += len;
			}

			parts2.Add((part.Value, null));

			prevIndex += part.Value switch
			{
				Parts.Mls => 3,
				Parts.Mcs => 3,
				Parts.Tick => 1,
				Parts.Nano => 2,
				_ => 2,
			};
		}

		_parts = [.. parts2];
	}

	/// <summary>
	/// Parses the specified input string into a <see cref="TimeSpan"/> value based on the parser's template.
	/// </summary>
	/// <param name="input">The string representation of a time span to parse.</param>
	/// <returns>A <see cref="TimeSpan"/> value that corresponds to the given input.</returns>
	/// <exception cref="InvalidCastException">Thrown when the input cannot be converted to a TimeSpan using the provided template.</exception>
	public TimeSpan Parse(string input)
	{
		try
		{
			var days = _dayStart == -1 ? 0 : (input[_dayStart] - '0') * 10 + (input[_dayStart + 1] - '0');

			var hours = _hourStart == -1 ? 0 : (input[_hourStart] - '0') * 10 + (input[_hourStart + 1] - '0');
			var minutes = _minuteStart == -1 ? 0 : (input[_minuteStart] - '0') * 10 + (input[_minuteStart + 1] - '0');
			var seconds = _secondStart == -1 ? 0 : (input[_secondStart] - '0') * 10 + (input[_secondStart + 1] - '0');

			var millis = _milliStart == -1 ? 0 : (input[_milliStart] - '0') * 100 + (input[_milliStart + 1] - '0') * 10 + (input[_milliStart + 2] - '0');
			var micro = _microStart == -1 ? 0 : (input[_microStart] - '0') * 100 + (input[_microStart + 1] - '0') * 10 + (input[_microStart + 2] - '0');
			var tick = _tickStart == -1 ? 0 : (input[_tickStart] - '0');
			var nano = _nanoStart == -1 ? 0 : (input[_nanoStart] - '0') * 10 + (input[_nanoStart + 1] - '0');

			var ts = new TimeSpan(days, hours, minutes, seconds, millis);

			var value = micro * 1000L + tick * 100L + nano;

			return ts.AddNanoseconds(value);
		}
		catch (Exception ex)
		{
			throw new InvalidCastException($"Cannot convert {input} with format {Template} to {typeof(TimeSpan).Name}.", ex);
		}
	}

	/// <summary>
	/// Formats the specified <see cref="TimeSpan"/> value into a string using the parser's template.
	/// </summary>
	/// <param name="value">The <see cref="TimeSpan"/> value to format.</param>
	/// <returns>A string representation of the <see cref="TimeSpan"/> value according to the template.</returns>
	public string ToString(TimeSpan value)
	{
		var builder = new StringBuilder();

		foreach (var part in _parts)
		{
			switch (part.part)
			{
				case Parts.String:
					builder.Append(part.format);
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
				case Parts.Mcs:
					Append(builder, value.GetMicroseconds(), 3);
					break;
				case Parts.Tick:
					Append(builder, value.GetNanoseconds() / 100, 1);
					break;
				case Parts.Nano:
					Append(builder, value.GetNanoseconds() % 100, 2);
					break;
				default:
					throw new InvalidOperationException(part.part.ToString());
			}
		}

		return builder.ToString();
	}

	private static void Append(StringBuilder builder, int value, int size = 2)
	{
		if (size == 1)
		{
			builder.Append((char)('0' + value));
		}
		else if (size == 2)
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

	/// <inheritdoc />
	public override string ToString() => Template;
}