namespace Ecng.Common
{
	public struct ValTuple<T1, T2>
	{
		public T1 Item1 {get;}
		public T2 Item2 {get;}

		public ValTuple(T1 i1, T2 i2)
		{
			Item1 = i1;
			Item2 = i2;
		}

		public override string ToString() => $"[{Item1},{Item2}]";
	}

	public struct ValTuple<T1, T2, T3>
	{
		public T1 Item1 {get;}
		public T2 Item2 {get;}
		public T3 Item3 {get;}

		public ValTuple(T1 i1, T2 i2, T3 i3)
		{
			Item1 = i1;
			Item2 = i2;
			Item3 = i3;
		}

		public override string ToString() => $"[{Item1},{Item2},{Item3}]";
	}

	public struct ValTuple<T1, T2, T3, T4>
	{
		public T1 Item1 {get;}
		public T2 Item2 {get;}
		public T3 Item3 {get;}
		public T4 Item4 {get;}

		public ValTuple(T1 i1, T2 i2, T3 i3, T4 i4)
		{
			Item1 = i1;
			Item2 = i2;
			Item3 = i3;
			Item4 = i4;
		}

		public override string ToString() => $"[{Item1},{Item2},{Item3},{Item4}]";
	}

	public struct ValTuple<T1, T2, T3, T4, T5>
	{
		public T1 Item1 {get;}
		public T2 Item2 {get;}
		public T3 Item3 {get;}
		public T4 Item4 {get;}
		public T5 Item5 {get;}

		public ValTuple(T1 i1, T2 i2, T3 i3, T4 i4, T5 i5)
		{
			Item1 = i1;
			Item2 = i2;
			Item3 = i3;
			Item4 = i4;
			Item5 = i5;
		}

		public override string ToString() => $"[{Item1},{Item2},{Item3},{Item4},{Item5}]";
	}

	public static class ValTuple
	{
		public static ValTuple<T1, T2> Create<T1, T2>(T1 i1, T2 i2) => new ValTuple<T1, T2>(i1, i2);

		public static ValTuple<T1, T2, T3> Create<T1, T2, T3>(T1 i1, T2 i2, T3 i3) => new ValTuple<T1, T2, T3>(i1, i2, i3);

		public static ValTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 i1, T2 i2, T3 i3, T4 i4) => new ValTuple<T1, T2, T3, T4>(i1, i2, i3, i4);

		public static ValTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 i1, T2 i2, T3 i3, T4 i4, T5 i5) => new ValTuple<T1, T2, T3, T4, T5>(i1, i2, i3, i4, i5);
	}
}