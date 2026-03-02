namespace Ecng.Data.Sql;

static class MethodVisitorRegistry
{
	private static readonly SynchronizedDictionary<MemberInfo, MethodVisitor> _visitors = [];
	private static readonly Dictionary<MemberInfo, Type> _genericVisitors = [];

	static MethodVisitorRegistry()
	{
		static void AddVisitor(MethodVisitor visitor)
		{
			foreach (var member in visitor.Members)
				_visitors.Add(member, visitor);
		}

		static void AddGenericVisitor(MethodVisitor visitor)
		{
			foreach (var member in visitor.Members)
				_genericVisitors.Add(member is MethodInfo mi ? mi.GetGenericMethodDefinition() : member, visitor.GetType().GetGenericTypeDefinition());
		}

		AddVisitor(new DateTimeNowVisitor());
		AddVisitor(new DateTimeUtcNowVisitor());
		AddVisitor(new DateTimeTodayVisitor());
		AddVisitor(new DateTimeOffsetNowVisitor());
		AddVisitor(new DateTimeOffsetUtcNowVisitor());
		AddVisitor(new DateTimeAddYearsVisitor());
		AddVisitor(new DateTimeAddMonthsVisitor());
		AddVisitor(new DateTimeAddDaysVisitor());
		AddVisitor(new DateTimeAddHoursVisitor());
		AddVisitor(new DateTimeAddMinutesVisitor());
		AddVisitor(new DateTimeAddSecondsVisitor());
		AddVisitor(new DateTimeAddMillisecondsVisitor());
		AddVisitor(new DateTimeOffsetAddYearsVisitor());
		AddVisitor(new DateTimeOffsetAddMonthsVisitor());
		AddVisitor(new DateTimeOffsetAddDaysVisitor());
		AddVisitor(new DateTimeOffsetAddHoursVisitor());
		AddVisitor(new DateTimeOffsetAddMinutesVisitor());
		AddVisitor(new DateTimeOffsetAddSecondsVisitor());
		AddVisitor(new DateTimeOffsetAddMillisecondsVisitor());
		AddVisitor(new DateTimeDateVisitor());
		AddVisitor(new DateTimeOffsetDateVisitor());
		AddVisitor(new DateTimeTimeOfDayVisitor());
		AddVisitor(new DateTimeOffsetTimeOfDayVisitor());
		AddVisitor(new DateTimeYearVisitor());
		AddVisitor(new DateTimeMonthVisitor());
		AddVisitor(new DateTimeDayVisitor());
		AddVisitor(new DateTimeHourVisitor());
		AddVisitor(new DateTimeMinuteVisitor());
		AddVisitor(new DateTimeSecondVisitor());
		AddVisitor(new DateTimeMillisecondVisitor());
		AddVisitor(new DateTimeOffsetYearVisitor());
		AddVisitor(new DateTimeOffsetMonthVisitor());
		AddVisitor(new DateTimeOffsetDayVisitor());
		AddVisitor(new DateTimeOffsetHourVisitor());
		AddVisitor(new DateTimeOffsetMinuteVisitor());
		AddVisitor(new DateTimeOffsetSecondVisitor());
		AddVisitor(new DateTimeOffsetMillisecondVisitor());
		AddVisitor(new DateTimeDayOfYearVisitor());
		AddVisitor(new DateTimeOffsetDayOfYearVisitor());
		AddVisitor(new StringEmptyVisitor());
		AddVisitor(new StringLengthVisitor());
		AddVisitor(new StringUpperVisitor());
		AddVisitor(new StringLowerVisitor());
		AddVisitor(new StringTrimVisitor());
		AddVisitor(new StringLTrimVisitor());
		AddVisitor(new StringRTrimVisitor());
		AddVisitor(new StringSubstringVisitor());
		AddVisitor(new StringConcatVisitor());
		AddVisitor(new StringIsNullOrEmptyVisitor());
		AddVisitor(new StringIsNullOrWhiteSpaceVisitor());
		AddVisitor(new StringFirstVisitor());
		AddVisitor(new StringFirstOrDefaultVisitor());
		AddVisitor(new StringLastVisitor());
		AddVisitor(new StringLastOrDefaultVisitor());
		AddVisitor(new StringIndexOfVisitor());
		AddVisitor(new StringReplaceVisitor());
		AddVisitor(new StringEqualsVisitor());
		AddVisitor(new StringCompareToVisitor());
		AddVisitor(new StringCompareVisitor());
		AddVisitor(new StringContainsVisitor());
		AddVisitor(new StringStartsWithVisitor());
		AddVisitor(new StringEndsWithVisitor());
		AddVisitor(new ByteArrayFirstVisitor());
		AddVisitor(new ByteArrayFirstOrDefaultVisitor());
		AddVisitor(new ByteArrayLastVisitor());
		AddVisitor(new ByteArrayLastOrDefaultVisitor());
		AddVisitor(new ByteArrayContainsVisitor());
		AddVisitor(new GuidNewGuidVisitor());
		AddVisitor(new RandVisitor());
		AddVisitor(new EnumHasFlagVisitor());
		AddVisitor(new LikeVisitor());
		AddVisitor(new IfNullVisitor());
		AddVisitor(new MathVisitor());
		AddVisitor(new TimeSpanPartVisitor());
		AddVisitor(new MaxVisitor());
		AddVisitor(new MinVisitor());
		AddVisitor(new AvgVisitor());
		AddVisitor(new SumVisitor());
		AddVisitor(new StringFormatVisitor());
		AddVisitor(new EqualsVisitor());

		AddVisitor(new DecimalCastVisitor());

		AddGenericVisitor(new IsNullVisitor<VoidType>());
		AddGenericVisitor(new IsNullFuncVisitor<VoidType>());
		AddGenericVisitor(new CountVisitor<VoidType>());
		AddGenericVisitor(new MaxVisitor<VoidType>());
		AddGenericVisitor(new MinVisitor<VoidType>());
		AddGenericVisitor(new AvgVisitor<VoidType>());
		AddGenericVisitor(new SumVisitor<VoidType>());
		AddGenericVisitor(new NullableValueVisitor<VoidType>());
		AddGenericVisitor(new ContainsVisitor<int>());
		AddGenericVisitor(new ConcatVisitor<VoidType>());
		AddGenericVisitor(new UnionVisitor<VoidType>());
		AddGenericVisitor(new FirstOrDefaultVisitor<VoidType>());
		AddGenericVisitor(new AnyVisitor<VoidType>());
		AddGenericVisitor(new SequenceEqualVisitor<int>());
	}

	public static bool TryGetVisitor(this MemberInfo mi, out MethodVisitor visitor)
	{
		if (_visitors.TryGetValue(mi, out visitor))
			return true;

		if (mi is PropertyInfo pi)
		{
			if (!pi.DeclaringType.IsGenericType)
				return false;

			if (!_genericVisitors.TryGetValue(pi.DeclaringType.GetGenericTypeDefinition().GetProperty(mi.Name), out var genVisitor1))
				return false;

			visitor = _visitors.SafeAdd(pi, key => genVisitor1.Make(pi.DeclaringType.GetGenericArguments()[0]).CreateInstance<MethodVisitor>());

			return true;
		}

		if (mi is not MethodInfo method || !method.IsGenericMethod)
			return false;

		var genMethod = method.GetGenericMethodDefinition();

		if (genMethod is null)
			return false;

		if (!_genericVisitors.TryGetValue(genMethod, out var genVisitor))
			return false;

		var type = method.GetGenericArguments()[0];

		visitor = _visitors.SafeAdd(genMethod.Make(type), key => genVisitor.Make(type).CreateInstance<MethodVisitor>());

		return true;
	}
}