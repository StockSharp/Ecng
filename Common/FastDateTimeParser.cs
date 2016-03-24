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
				var years = _yearStart == -1 ? DateTime.Now.Year : (input[_yearStart] - '0') * 1000 + (input[_yearStart + 1] - '0') * 100 + (input[_yearStart + 2] - '0') * 10 + (input[_yearStart + 3] - '0');
				var months = _monthStart == -1 ? DateTime.Now.Month : (input[_monthStart] - '0') * 10 + (input[_monthStart + 1] - '0');
				var days = _dayStart == -1 ? DateTime.Now.Year : (input[_dayStart] - '0') * 10 + (input[_dayStart + 1] - '0');

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
	}
}