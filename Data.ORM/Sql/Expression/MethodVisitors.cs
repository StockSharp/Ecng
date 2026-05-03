namespace Ecng.Data.Sql;

abstract class MethodVisitor(IEnumerable<MemberInfo> members)
{
	public IEnumerable<MemberInfo> Members { get; } = members.ToArray();

	public abstract void Visit(ExpressionQueryTranslator translator, Expression expression);
}

abstract class MethodVisitor<T>(string name) : MethodVisitor(typeof(T).GetMember(name))
{
}

abstract class DateAddVisitor<T>(string name, string firstArg) : MethodVisitor<T>(name)
{
	private readonly string _firstArg = firstArg;

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;
		var mce = (MethodCallExpression)expression;

		// Capture amount sub-expression into a temporary query
		var amountQuery = new Query();
		translator.Context.Curr = amountQuery;
		translator.Visit(mce.Arguments[0]);

		// Capture source sub-expression into a temporary query
		var sourceQuery = new Query();
		translator.Context.Curr = sourceQuery;
		translator.Visit(mce.Object);

		// Restore and emit dialect-aware DATEADD
		translator.Context.Curr = q;
		q.AddAction((d, sb) => d.AppendDateAdd(sb, _firstArg, amountQuery.Render(d), sourceQuery.Render(d)));
	}
}

class DateTimeNowVisitor : MethodVisitor<DateTime>
{
	public DateTimeNowVisitor()
		: base(nameof(DateTime.Now))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.Now();
}

class DateTimeUtcNowVisitor : MethodVisitor<DateTime>
{
	public DateTimeUtcNowVisitor()
		: base(nameof(DateTime.UtcNow))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.UtcNow();
}

class DateTimeTodayVisitor : MethodVisitor<DateTime>
{
	public DateTimeTodayVisitor()
		: base(nameof(DateTime.Today))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr
				.Convert()
				.OpenBracket()
					.Raw("date")
					.Comma()
					.Now()
				.CloseBracket();
}

class DateTimeOffsetNowVisitor : MethodVisitor<DateTimeOffset>
{
	public DateTimeOffsetNowVisitor()
		: base(nameof(DateTimeOffset.Now))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.SysNow();
}

class DateTimeOffsetUtcNowVisitor : MethodVisitor<DateTimeOffset>
{
	public DateTimeOffsetUtcNowVisitor()
		: base(nameof(DateTime.UtcNow))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.SysUtcNow();
}

class DateTimeAddYearsVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddYearsVisitor()
		: base(nameof(DateTime.AddYears), "year")
	{
	}
}

class DateTimeAddMonthsVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddMonthsVisitor()
		: base(nameof(DateTime.AddMonths), "month")
	{
	}
}

class DateTimeAddDaysVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddDaysVisitor()
		: base(nameof(DateTime.AddDays), "day")
	{
	}
}

class DateTimeAddHoursVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddHoursVisitor()
		: base(nameof(DateTime.AddHours), "hour")
	{
	}
}

class DateTimeAddMinutesVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddMinutesVisitor()
		: base(nameof(DateTime.AddMinutes), "minute")
	{
	}
}

class DateTimeAddSecondsVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddSecondsVisitor()
		: base(nameof(DateTime.AddSeconds), "second")
	{
	}
}

class DateTimeAddMillisecondsVisitor : DateAddVisitor<DateTime>
{
	public DateTimeAddMillisecondsVisitor()
		: base(nameof(DateTime.AddMilliseconds), "millisecond")
	{
	}
}

class DateTimeOffsetAddYearsVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddYearsVisitor()
		: base(nameof(DateTimeOffset.AddYears), "year")
	{
	}
}

class DateTimeOffsetAddMonthsVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddMonthsVisitor()
		: base(nameof(DateTimeOffset.AddMonths), "month")
	{
	}
}

class DateTimeOffsetAddDaysVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddDaysVisitor()
		: base(nameof(DateTimeOffset.AddDays), "day")
	{
	}
}

class DateTimeOffsetAddHoursVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddHoursVisitor()
		: base(nameof(DateTimeOffset.AddHours), "hour")
	{
	}
}

class DateTimeOffsetAddMinutesVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddMinutesVisitor()
		: base(nameof(DateTimeOffset.AddMinutes), "minute")
	{
	}
}

class DateTimeOffsetAddSecondsVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddSecondsVisitor()
		: base(nameof(DateTimeOffset.AddSeconds), "second")
	{
	}
}

class DateTimeOffsetAddMillisecondsVisitor : DateAddVisitor<DateTimeOffset>
{
	public DateTimeOffsetAddMillisecondsVisitor()
		: base(nameof(DateTimeOffset.AddMilliseconds), "millisecond")
	{
	}
}

abstract class DateConvertVisitor<T>(string name, string firstArg) : MethodVisitor<T>(name)
{
	private readonly string _firstArg = firstArg;

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.Convert()
			.OpenBracket()
			.Raw(_firstArg)
			.Comma();

		var me = (MemberExpression)expression;
		translator.Visit(me.Expression);

		q.CloseBracket();
	}
}

class DateTimeDateVisitor : DateConvertVisitor<DateTime>
{
	public DateTimeDateVisitor()
		: base(nameof(DateTime.Date), "date")
	{
	}
}

class DateTimeTimeOfDayVisitor : DateConvertVisitor<DateTime>
{
	public DateTimeTimeOfDayVisitor()
		: base(nameof(DateTime.TimeOfDay), "time")
	{
	}
}

class DateTimeOffsetDateVisitor : DateConvertVisitor<DateTimeOffset>
{
	public DateTimeOffsetDateVisitor()
		: base(nameof(DateTimeOffset.Date), "date")
	{
	}
}

class DateTimeOffsetTimeOfDayVisitor : DateConvertVisitor<DateTimeOffset>
{
	public DateTimeOffsetTimeOfDayVisitor()
		: base(nameof(DateTimeOffset.TimeOfDay), "time")
	{
	}
}

abstract class DatePartVisitor<T>(string name, string firstArg) : MethodVisitor<T>(name)
{
	private readonly string _firstArg = firstArg;

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.AddAction((d, sb) => d.AppendDatePartOpen(sb, _firstArg));

		var me = (MemberExpression)expression;
		translator.Visit(me.Expression);

		q.AddAction((d, sb) => d.AppendDatePartClose(sb));
	}
}

class DateTimeYearVisitor : DatePartVisitor<DateTime>
{
	public DateTimeYearVisitor()
		: base(nameof(DateTime.Year), "year")
	{
	}
}

class DateTimeMonthVisitor : DatePartVisitor<DateTime>
{
	public DateTimeMonthVisitor()
		: base(nameof(DateTime.Month), "month")
	{
	}
}

class DateTimeDayVisitor : DatePartVisitor<DateTime>
{
	public DateTimeDayVisitor()
		: base(nameof(DateTime.Day), "day")
	{
	}
}

class DateTimeHourVisitor : DatePartVisitor<DateTime>
{
	public DateTimeHourVisitor()
		: base(nameof(DateTime.Hour), "hour")
	{
	}
}

class DateTimeMinuteVisitor : DatePartVisitor<DateTime>
{
	public DateTimeMinuteVisitor()
		: base(nameof(DateTime.Minute), "minute")
	{
	}
}

class DateTimeSecondVisitor : DatePartVisitor<DateTime>
{
	public DateTimeSecondVisitor()
		: base(nameof(DateTime.Second), "second")
	{
	}
}

class DateTimeMillisecondVisitor : DatePartVisitor<DateTime>
{
	public DateTimeMillisecondVisitor()
		: base(nameof(DateTime.Millisecond), "millisecond")
	{
	}
}

class DateTimeDayOfYearVisitor : DatePartVisitor<DateTime>
{
	public DateTimeDayOfYearVisitor()
		: base(nameof(DateTime.DayOfYear), "dayofyear")
	{
	}
}

class DateTimeOffsetYearVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetYearVisitor()
		: base(nameof(DateTimeOffset.Year), "year")
	{
	}
}

class DateTimeOffsetMonthVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetMonthVisitor()
		: base(nameof(DateTimeOffset.Month), "month")
	{
	}
}

class DateTimeOffsetDayVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetDayVisitor()
		: base(nameof(DateTimeOffset.Day), "day")
	{
	}
}

class DateTimeOffsetHourVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetHourVisitor()
		: base(nameof(DateTimeOffset.Hour), "hour")
	{
	}
}

class DateTimeOffsetMinuteVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetMinuteVisitor()
		: base(nameof(DateTimeOffset.Minute), "minute")
	{
	}
}

class DateTimeOffsetSecondVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetSecondVisitor()
		: base(nameof(DateTimeOffset.Second), "second")
	{
	}
}

class DateTimeOffsetMillisecondVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetMillisecondVisitor()
		: base(nameof(DateTimeOffset.Millisecond), "millisecond")
	{
	}
}

class DateTimeOffsetDayOfYearVisitor : DatePartVisitor<DateTimeOffset>
{
	public DateTimeOffsetDayOfYearVisitor()
		: base(nameof(DateTimeOffset.DayOfYear), "dayofyear")
	{
	}
}

class StringEmptyVisitor : MethodVisitor<string>
{
	public StringEmptyVisitor()
		: base(nameof(string.Empty))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}''"));
}

class StringLengthVisitor : MethodVisitor<string>
{
	public StringLengthVisitor()
		: base(nameof(string.Length))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.Len()
			.OpenBracket();

		var me = (MemberExpression)expression;
		translator.Visit(me.Expression);

		q.CloseBracket();
	}
}

class StringUpperVisitor : MethodVisitor<string>
{
	public StringUpperVisitor()
		: base(nameof(string.ToUpper))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.Upper()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.CloseBracket();
	}
}

class StringLowerVisitor : MethodVisitor<string>
{
	public StringLowerVisitor()
		: base(nameof(string.ToLower))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.Lower()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.CloseBracket();
	}
}

class StringTrimVisitor : MethodVisitor<string>
{
	public StringTrimVisitor()
		: base(nameof(string.Trim))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.AddAction((d, sb) => d.AppendTrimOpen(sb));

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.AddAction((d, sb) => d.AppendTrimClose(sb));
	}
}

class StringLTrimVisitor : MethodVisitor<string>
{
	public StringLTrimVisitor()
		: base(nameof(string.TrimStart))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.LTrim()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.CloseBracket();
	}
}

class StringRTrimVisitor : MethodVisitor<string>
{
	public StringRTrimVisitor()
		: base(nameof(string.TrimEnd))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.RTrim()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.CloseBracket();
	}
}

class StringSubstringVisitor : MethodVisitor<string>
{
	public StringSubstringVisitor()
		: base(nameof(string.Substring))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.SubString()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.Comma();

		translator.Visit(mce.Arguments[0]);

		q.Raw(" + 1");
		q.Comma();

		if (mce.Method.GetParameters().Length == 1)
		{
			q.Len().OpenBracket();
			translator.Visit(mce.Object);
			q.CloseBracket();
		}
		else
			translator.Visit(mce.Arguments[1]);

		q.CloseBracket();
	}
}

abstract class EnumerableVisitor<T>(string name) : MethodVisitor(typeof(Enumerable).GetMember(name).OfType<MethodInfo>().Where(m => m.GetGenericArguments().Length == 1).Select(m => m.Make(typeof(T))))
{
}

abstract class EnumerableFirstLastVisitor<T>(bool isLast, string name) : EnumerableVisitor<T>(name)
{
	private readonly bool _isLast = isLast;

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.SubString()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);

		q.Comma();

		if (_isLast)
		{
			q.Len().OpenBracket();
			translator.Visit(mce.Arguments[0]);
			q.CloseBracket();
		}
		else
			q.Raw("1");

		q.Comma();
		q.Raw("1");

		q.CloseBracket();
	}
}

abstract class CharEnumerableVisitor(bool isLast, string name) : EnumerableFirstLastVisitor<char>(isLast, name)
{
}

class StringFirstVisitor : CharEnumerableVisitor
{
	public StringFirstVisitor()
		: base(false, nameof(Enumerable.First))
	{
	}
}

class StringFirstOrDefaultVisitor : CharEnumerableVisitor
{
	public StringFirstOrDefaultVisitor()
		: base(false, nameof(Enumerable.FirstOrDefault))
	{
	}
}

class StringLastVisitor : CharEnumerableVisitor
{
	public StringLastVisitor()
		: base(true, nameof(Enumerable.Last))
	{
	}
}

class StringLastOrDefaultVisitor : CharEnumerableVisitor
{
	public StringLastOrDefaultVisitor()
		: base(true, nameof(Enumerable.LastOrDefault))
	{
	}
}

class StringConcatVisitor : MethodVisitor<string>
{
	public StringConcatVisitor()
		: base(nameof(string.Concat))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		var idx = 0;

		foreach (var arg in mce.Arguments)
		{
			translator.Visit(arg);

			idx++;

			if (idx < mce.Arguments.Count)
				q.Concat();
		}

		q.CloseBracket();
	}
}

class StringIsNullOrEmptyVisitor : MethodVisitor<string>
{
	public StringIsNullOrEmptyVisitor()
		: base(nameof(string.IsNullOrEmpty))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		
		translator.Visit(mce.Arguments[0]);
		q.Is().Null();

		q.Or();
		translator.Visit(mce.Arguments[0]);
		q.Like().AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}''"));

		q.CloseBracket();
	}
}

class StringIsNullOrWhiteSpaceVisitor : MethodVisitor<string>
{
	public StringIsNullOrWhiteSpaceVisitor()
		: base(nameof(string.IsNullOrWhiteSpace))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;

		translator.Visit(mce.Arguments[0]);
		q.Is().Null();

		q.Or();
		translator.Visit(mce.Arguments[0]);
		q.Equal().AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}''"));

		q.CloseBracket();
	}
}

class StringHelperIsEmptyVisitor : MethodVisitor
{
	public StringHelperIsEmptyVisitor()
		: base(typeof(StringHelper).GetMember(nameof(StringHelper.IsEmpty))
			.OfType<MethodInfo>()
			.Where(m => m.GetParameters().Length == 1))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;

		translator.Visit(mce.Arguments[0]);
		q.Is().Null();

		q.Or();
		translator.Visit(mce.Arguments[0]);
		q.Like().AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}''"));

		q.CloseBracket();
	}
}

class StringHelperIsEmptyOrWhiteSpaceVisitor : MethodVisitor
{
	public StringHelperIsEmptyOrWhiteSpaceVisitor()
		: base(typeof(StringHelper).GetMember(nameof(StringHelper.IsEmptyOrWhiteSpace))
			.OfType<MethodInfo>()
			.Where(m => m.GetParameters().Length == 1))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;

		translator.Visit(mce.Arguments[0]);
		q.Is().Null();

		q.Or();
		translator.Visit(mce.Arguments[0]);
		q.Equal().AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}''"));

		q.CloseBracket();
	}
}

class StringIndexOfVisitor : MethodVisitor<string>
{
	public StringIndexOfVisitor()
		: base(nameof(string.IndexOf))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q
			.OpenBracket()
			.CharIndex()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;

		foreach (var arg in mce.Arguments)
		{
			translator.Visit(arg);
			q.Comma();
		}

		translator.Visit(mce.Object);

		q
			.CloseBracket()
			.Raw(" - 1")
			.CloseBracket();
	}
}

class StringReplaceVisitor : MethodVisitor<string>
{
	public StringReplaceVisitor()
		: base(nameof(string.Replace))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q
			.Replace()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		foreach (var arg in mce.Arguments)
		{
			q.Comma();
			translator.Visit(arg);
		}

		q.CloseBracket();
	}
}

class StringEqualsVisitor : MethodVisitor<string>
{
	public StringEqualsVisitor()
		: base(nameof(string.Equals))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.Equal();

		translator.Visit(mce.Arguments[0]);

		q.CloseBracket();
	}
}

class StringCompareToVisitor : MethodVisitor<string>
{
	public StringCompareToVisitor()
		: base(nameof(string.CompareTo))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		q.Raw("case when ");

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.Equal();

		translator.Visit(mce.Arguments[0]);

		q.Raw(" then 0 else 1 end");

		q.CloseBracket();
	}
}

class StringCompareVisitor : MethodVisitor<string>
{
	public StringCompareVisitor()
		: base(nameof(string.Compare))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		q.Raw("case when ");

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);

		q.Equal();

		translator.Visit(mce.Arguments[1]);

		q.Raw(" then 0 else 1 end");

		q.CloseBracket();
	}
}

class StringContainsVisitor : MethodVisitor<string>
{
	public StringContainsVisitor()
		: base(nameof(string.Contains))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.Like();

		q.AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}'%'"));
		q.Concat();
		translator.Visit(mce.Arguments[0]);
		q.Concat();
		q.AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}'%'"));

		q.CloseBracket();
	}
}

class StringStartsWithVisitor : MethodVisitor<string>
{
	public StringStartsWithVisitor()
		: base(nameof(string.StartsWith))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.Like();

		translator.Visit(mce.Arguments[0]);
		q.Concat();
		q.AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}'%'"));

		q.CloseBracket();
	}
}

class StringEndsWithVisitor : MethodVisitor<string>
{
	public StringEndsWithVisitor()
		: base(nameof(string.EndsWith))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.Like();

		q.AddAction((d, sb) => sb.Append($"{d.UnicodePrefix}'%'"));
		q.Concat();
		translator.Visit(mce.Arguments[0]);

		q.CloseBracket();
	}
}

class SequenceEqualVisitor<T> : MethodVisitor
{
	private static IEnumerable<MemberInfo> GetMembers(string name)
	{
		var found = typeof(MemoryExtensions).GetMember(name).Concat(typeof(Queryable).GetMember(name)).Concat(typeof(Enumerable).GetMember(name)).OfType<MethodInfo>().Where(m => m.GetGenericArguments().Length == 1);

		if (!typeof(T).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>)))
		{
			found = found.Where(m => m.GetParameters().Length == 3);
		}

		return found.Select(m => m.Make(typeof(T)));
	}

	public SequenceEqualVisitor()
		: base(GetMembers(nameof(MemoryExtensions.SequenceEqual)))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;

		translator.Visit(mce.Arguments[0].TryHandleImplicit());
		q.Equal();
		translator.Visit(mce.Arguments[1].TryHandleImplicit());

		q.CloseBracket();
	}
}

abstract class ByteEnumerableVisitor(bool isLast, string name) : EnumerableFirstLastVisitor<byte>(isLast, name)
{
}

class ByteArrayFirstVisitor : ByteEnumerableVisitor
{
	public ByteArrayFirstVisitor()
		: base(false, nameof(Enumerable.First))
	{
	}
}

class ByteArrayFirstOrDefaultVisitor : ByteEnumerableVisitor
{
	public ByteArrayFirstOrDefaultVisitor()
		: base(false, nameof(Enumerable.FirstOrDefault))
	{
	}
}

class ByteArrayLastVisitor : ByteEnumerableVisitor
{
	public ByteArrayLastVisitor()
		: base(true, nameof(Enumerable.Last))
	{
	}
}

class ByteArrayLastOrDefaultVisitor : ByteEnumerableVisitor
{
	public ByteArrayLastOrDefaultVisitor()
		: base(true, nameof(Enumerable.LastOrDefault))
	{
	}
}

class ByteArrayContainsVisitor : EnumerableVisitor<byte>
{
	public ByteArrayContainsVisitor()
		: base(nameof(Enumerable.Contains))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q
			.OpenBracket()
			.CharIndex()
			.OpenBracket();

		var mce = (MethodCallExpression)expression;

		translator.Visit(mce.Arguments[1]);
		q.Comma();
		translator.Visit(mce.Arguments[0]);

		q
			.CloseBracket()
			.Raw(" > 0")
			.CloseBracket();
	}
}

class GuidNewGuidVisitor : MethodVisitor<Guid>
{
	public GuidNewGuidVisitor()
		: base(nameof(Guid.NewGuid))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.NewId();
}

class RandVisitor : MethodVisitor<Random>
{
	public RandVisitor()
		: base(nameof(Random.NextDouble))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Context.Curr.Rand();
}

class EnumHasFlagVisitor : MethodVisitor<Enum>
{
	public EnumHasFlagVisitor()
		: base(nameof(Enum.HasFlag))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Object);

		q.BitwiseAnd();

		translator.Visit(mce.Arguments[0]);

		q.CloseBracket();

		q.Equal();

		translator.Visit(mce.Arguments[0]);

		q.CloseBracket();
	}
}

abstract class SqlFunctionVisitor(string name) : MethodVisitor(typeof(SqlFunctions).GetMember(name))
{
}

class LikeVisitor : SqlFunctionVisitor
{
	public LikeVisitor()
		: base(nameof(SqlFunctions.Like))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);

		q.Like();

		translator.Visit(mce.Arguments[1]);

		q.CloseBracket();
	}
}

class IfNullVisitor : SqlFunctionVisitor
{
	public IfNullVisitor()
		: base(nameof(SqlFunctions.IfNull))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.NullIf();
		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);

		q.Comma();

		translator.Visit(mce.Arguments[1]);

		q.CloseBracket();
	}
}

class IsNullVisitor<T> : MethodVisitor
{
	public IsNullVisitor()
		: base(typeof(NullableHelper).GetMember(nameof(NullableHelper.IsNull)).OfType<MethodInfo>().Where(m => m.GetGenericArguments().Length == 1).Select(m => m.Make(typeof(T))))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);

		q.Is().Null().CloseBracket();
	}
}

class IsNullFuncVisitor<T> : MethodVisitor
{
	public IsNullFuncVisitor()
		: base(typeof(SqlFunctions).GetMember(nameof(SqlFunctions.IsNull)).OfType<MethodInfo>().Select(m => m.Make(typeof(T))))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.IsNull().OpenBracket();

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);

		q.Comma();

		translator.Visit(mce.Arguments[1]);

		q.CloseBracket();
	}
}

class MathVisitor : MethodVisitor
{
	public MathVisitor()
		: base(typeof(Math).GetMembers())
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;
		var mce = (MethodCallExpression)expression;

		var methodName = mce.Method.Name;

		if (mce.Method.Name == nameof(Math.Truncate))
			methodName = nameof(Math.Round);

		q.Raw(methodName).OpenBracket();

		var idx = 0;

		foreach (var arg in mce.Arguments)
		{
			translator.Visit(arg);

			idx++;

			if (idx < mce.Arguments.Count)
				q.Comma();
		}

		if (mce.Method.Name == nameof(Math.Truncate))
		{
			q.Comma().Raw("0").Comma().Raw("1");
		}
		else if (mce.Method.Name == nameof(Math.Round) && mce.Method.GetParameters().Length == 1)
		{
			q.Comma().Raw("0");
		}

		q.CloseBracket();
	}
}

class TimeSpanPartVisitor : MethodVisitor
{
	public TimeSpanPartVisitor()
		: base(typeof(TimeSpan).GetProperties())
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		q.DateDiff();
		q.OpenBracket();

		var me = (MemberExpression)expression;

		var part = GetDatePart(me.Member.Name);

		q.Raw(part);
		q.Comma();

		if (me.Expression is BinaryExpression be)
		{
			translator.Visit(be.Right);
			q.Comma();
			translator.Visit(be.Left);
		}
		else
			translator.Visit(me.Expression);

		q.CloseBracket();
	}

	private static string GetDatePart(string member)
	{
		return member switch
		{
			nameof(TimeHelper.TotalYears) => "year",
			nameof(TimeHelper.TotalMonths) => "month",
			nameof(TimeHelper.TotalWeeks) => "week",
			nameof(TimeSpan.TotalDays) => "day",
			nameof(TimeSpan.TotalHours) => "hour",
			nameof(TimeSpan.TotalMinutes) => "minute",
			nameof(TimeSpan.TotalSeconds) => "second",
			nameof(TimeSpan.TotalMilliseconds) => "millisecond",
			_ => throw new ArgumentOutOfRangeException(nameof(member))
		};
	}
}

abstract class EnumerableAndQueryableVisitor(string name) : MethodVisitor(typeof(Enumerable).GetMember(name).Concat(typeof(Queryable).GetMember(name)).OfType<MethodInfo>().Where(m => !m.IsGenericMethodDefinition))
{
}

abstract class EnumerableAndQueryableVisitor<T>(string name) : MethodVisitor(typeof(Enumerable).GetMember(name).Concat(typeof(Queryable).GetMember(name)).OfType<MethodInfo>().Where(m => m.GetGenericArguments().Length == 1).Select(m => m.Make(typeof(T))))
{
	protected Type ItemType => GetType().GetGenericArguments()[0];
}

class CountVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	public CountVisitor()
		: base(nameof(Queryable.Count))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		var mce = (MethodCallExpression)expression;

		if (mce.Arguments.Count > 0 && mce.Arguments[0].NodeType != ExpressionType.Parameter)
		{
			translator.Visit(mce.Arguments[0]);
		}
		else
			q.Count().OpenBracket().Star().CloseBracket();
	}
}

abstract class BaseGroupFuncVisitor(Func<Query, Query> func)
{
	private readonly Func<Query, Query> _func = func;

	public void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;
		var mce = (MethodCallExpression)expression;

		var isInline = mce.Arguments.Count == 2;

		if (isInline)
			_func(q).OpenBracket();
		else
		{
			translator.WrapColumn.Enqueue((b, q) =>
			{
				if (b)
					_func(q).OpenBracket();
				else
					q.CloseBracket();
			});
		}

		translator.Visit(mce.Arguments[0]);

		if (isInline)
		{
			translator.Visit(mce.Arguments[1]);
			q.CloseBracket();
		}
		else
			translator.WrapColumn.Dequeue();
	}
}

class MaxVisitorVisitor : BaseGroupFuncVisitor
{
	public MaxVisitorVisitor()
		: base(q => q.Max())
	{
	}
}

class MaxVisitor : EnumerableAndQueryableVisitor
{
	private readonly MaxVisitorVisitor _visitor = new();

	public MaxVisitor()
		: base(nameof(Enumerable.Max))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class MaxVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	private readonly MaxVisitorVisitor _visitor = new();

	public MaxVisitor()
		: base(nameof(Queryable.Max))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class MinVisitorVisitor : BaseGroupFuncVisitor
{
	public MinVisitorVisitor()
		: base(q => q.Min())
	{
	}
}

class MinVisitor : EnumerableAndQueryableVisitor
{
	private readonly MinVisitorVisitor _visitor = new();

	public MinVisitor()
		: base(nameof(Enumerable.Min))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class MinVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	private readonly MinVisitorVisitor _visitor = new();

	public MinVisitor()
		: base(nameof(Queryable.Min))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class AvgVisitorVisitor : BaseGroupFuncVisitor
{
	public AvgVisitorVisitor()
		: base(q => q.Avg())
	{
	}
}

class AvgVisitor : EnumerableAndQueryableVisitor
{
	private readonly AvgVisitorVisitor _visitor = new();

	public AvgVisitor()
		: base(nameof(Enumerable.Average))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class AvgVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	private readonly AvgVisitorVisitor _visitor = new();

	public AvgVisitor()
		: base(nameof(Enumerable.Average))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class SumVisitorVisitor : BaseGroupFuncVisitor
{
	public SumVisitorVisitor()
		: base(q => q.Sum())
	{
	}
}

class SumVisitor : EnumerableAndQueryableVisitor
{
	private readonly SumVisitorVisitor _visitor = new();

	public SumVisitor()
		: base(nameof(Enumerable.Sum))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class SumVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	private readonly SumVisitorVisitor _visitor = new();

	public SumVisitor()
		: base(nameof(Enumerable.Sum))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> _visitor.Visit(translator, expression);
}

class NullableValueVisitor<T> : MethodVisitor
{
	public NullableValueVisitor()
		: base(typeof(Nullable<>).GetMember(nameof(Nullable<int>.Value)))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
		=> translator.Visit(((MemberExpression)expression).Expression);
}

class DecimalCastVisitor : MethodVisitor
{
	public DecimalCastVisitor()
		: base(typeof(decimal).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(mi => mi.Name == "op_Implicit"))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;

		translator.WrapColumn.Enqueue((b, q) =>
		{
			if (b)
				q.Cast().OpenBracket();
			else
				q.As().Raw("decimal(18,2)").CloseBracket();
		});

		translator.Visit(expression);

		translator.WrapColumn.Dequeue();
	}
}

class ContainsVisitor<T> : MethodVisitor
{
	private static IEnumerable<MemberInfo> GetMembers(string name)
	{
		var found = typeof(MemoryExtensions).GetMember(name).Concat(typeof(Queryable).GetMember(name)).OfType<MethodInfo>().Where(m => m.GetGenericArguments().Length == 1);

		if (!typeof(T).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>)))
		{
			found = found.Where(m => m.GetParameters().Length == 3);
		}

		if (typeof(T) == typeof(string))
		{
			found = found.Where(m => m.GetParameters()[0].ParameterType.GetGenericArguments()[0] == typeof(char));
		}
		else
		{
			found = found.Where(m => m.GetParameters()[0].ParameterType.GetGenericArguments()[0] != typeof(char));
		}

		return found.Select(m => m.Make(typeof(T)));
	}

	public ContainsVisitor()
		: base(GetMembers(nameof(MemoryExtensions.Contains)))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var q = translator.Context.Curr;
		var mce = (MethodCallExpression)expression;

		translator.Visit(mce.Arguments[1]);

		q.In();

		q.OpenBracket();

		var ctx = translator.Context;

		var firstArg = mce.Arguments[0].TryHandleImplicit();

		var isSubQuery = firstArg.NodeType == ExpressionType.Call;

		if (isSubQuery)
		{
			// Walk to the deepest source so TableAlias can be set BEFORE
			// `Visit(firstArg)` — otherwise inner-scope references like
			// `pgp.X` inside the sub-query body resolve through the
			// outer's alias and emit `[outer].[X]` instead of `[pgp].[X]`.
			var deepest = (MethodCallExpression)firstArg;
			while (deepest.Arguments.Count > 0 && deepest.Arguments[0] is MethodCallExpression deeper)
				deepest = deeper;

			string subAlias = null;
			if (deepest.Arguments.Count > 1 && deepest.Arguments[1].StripQuotes() is LambdaExpression lambda && lambda.Parameters.Count > 0)
				subAlias = lambda.Parameters[0].Name;

			translator.Context = new()
			{
				Curr = new(),
				ParamCountOffset = ctx.Parameters.Count + ctx.ParamCountOffset,
				TableAlias = subAlias,
			};

			// Inherit outer alias bindings so a correlated reference like
			// `outerParam.Field` inside the sub-query keeps the outer FROM
			// alias instead of being remapped to the sub-query's alias.
			foreach (var kv in ctx.Aliases)
				translator.Context.Aliases[kv.Key] = kv.Value;

			foreach (var jp in ctx.JoinParts)
				translator.Context.JoinParts.Add(jp);

			foreach (var preName in ctx.PreknownJoinAliases)
				translator.Context.PreknownJoinAliases.Add(preName);
		}

		translator.Visit(firstArg);

		if (isSubQuery)
		{
			while (firstArg is MethodCallExpression mce1)
			{
				mce = mce1;
				firstArg = mce.Arguments[0];
			}

			var type = mce.Method.ReturnType.GenericTypeArguments[0];

			translator.Context.Build(SchemaRegistry.Get(type)).CopyTo(q);

			ctx.AddParamsFromSubquery(translator.Context.Parameters, false);

			translator.Context = ctx;
		}

		q.CloseBracket();
	}
}

class ConcatVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	public ConcatVisitor()
		: base(nameof(Queryable.Concat))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var mce = (MethodCallExpression)expression;
		var ctx = translator.Context;
		var schema = SchemaRegistry.Get(ItemType);

		if (ctx.Curr is null)
			ctx.Curr = ctx.UnionPart;

		var idx = 0;

		foreach (var arg in mce.Arguments)
		{
			translator.Context = new() { ParamCountOffset = ctx.Parameters.Count + ctx.ParamCountOffset };
			translator.Visit(arg);

			translator.Context.Build(schema).CopyTo(ctx.Curr);

			if (++idx < mce.Arguments.Count)
				ctx.Curr.NewLine().UnionAll().NewLine();

			ctx.AddParamsFromSubquery(translator.Context.Parameters, false);
		}

		translator.Context = ctx;
	}
}

class UnionVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	public UnionVisitor()
		: base(nameof(Queryable.Union))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var mce = (MethodCallExpression)expression;
		var ctx = translator.Context;
		var schema = SchemaRegistry.Get(ItemType);

		if (ctx.Curr is null)
			ctx.Curr = ctx.UnionPart;

		var idx = 0;

		foreach (var arg in mce.Arguments)
		{
			translator.Context = new() { ParamCountOffset = ctx.Parameters.Count + ctx.ParamCountOffset };
			translator.Visit(arg);

			translator.Context.Build(schema).CopyTo(ctx.Curr);

			if (++idx < mce.Arguments.Count)
				ctx.Curr.NewLine().Union().NewLine();

			ctx.AddParamsFromSubquery(translator.Context.Parameters, false);
		}

		translator.Context = ctx;
	}
}

class FirstOrDefaultVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	public FirstOrDefaultVisitor()
		: base(nameof(Queryable.FirstOrDefault))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var mce = (MethodCallExpression)expression;
		translator.Context.Take = 1;
		translator.Visit(mce.Arguments[0]);
	}
}

class AnyVisitor<T> : EnumerableAndQueryableVisitor<T>
{
	public AnyVisitor()
		: base(nameof(Queryable.Any))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		translator.Context.Exists = true;

		var mce = (MethodCallExpression)expression;
		translator.Visit(mce.Arguments[0]);
	}
}

class StringFormatVisitor : MethodVisitor<string>
{
	public StringFormatVisitor()
		: base(nameof(string.Format))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var mce = (MethodCallExpression)expression;
		var q = translator.Context.Curr;

		q.FormatMessage();
		q.OpenBracket();

		var str = mce.Arguments[0].GetValue<string>();

		for (int i = 0; i < mce.Arguments.Count - 1; i++)
			str = str.Replace($"{{{i}}}", "%s");

		q.Raw($"'{str}'");

		foreach (var arg in mce.Arguments.Skip(1))
		{
			q.Comma();
			translator.Visit(arg);
		}

		q.CloseBracket();
	}
}

class EqualsVisitor : MethodVisitor<object>
{
	public EqualsVisitor()
		: base(nameof(object.Equals))
	{
	}

	public override void Visit(ExpressionQueryTranslator translator, Expression expression)
	{
		var mce = (MethodCallExpression)expression;
		var q = translator.Context.Curr;

		q.OpenBracket();

		translator.Visit(mce.Object);
		q.Equal();

		translator.Visit(mce.Arguments[0]);

		q.CloseBracket();
	}
}