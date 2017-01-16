namespace Ecng.Common
{
	using System;

	public class FastDateTimeParser
	{
		private readonly string _template;

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
	
		public FastDateTimeParser(string template)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException(nameof(template));

			_template = template;

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
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(input, _template, typeof(DateTime).Name), ex);
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
	}
}