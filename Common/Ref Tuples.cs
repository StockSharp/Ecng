namespace Ecng.Common
{
	public class RefPair<TFirst, TSecond>
	{
		public RefPair()
		{
		}

		public RefPair(TFirst first, TSecond second)
		{
			First = first;
			Second = second;
		}

		public TFirst First { get; set; }
		public TSecond Second { get; set; }
	}

	public class RefTriple<TFirst, TSecond, TThird> : RefPair<TFirst, TSecond>
	{
		public RefTriple()
		{
		}

		public RefTriple(TFirst first, TSecond second, TThird third)
			: base(first, second)
		{
			Third = third;
		}

		public TThird Third { get; set; }
	}

	public class RefQuadruple<TFirst, TSecond, TThird, TFourth> : RefTriple<TFirst, TSecond, TThird>
	{
		public RefQuadruple()
		{
		}

		public RefQuadruple(TFirst first, TSecond second, TThird third, TFourth fourth)
			: base(first, second, third)
		{
			Fourth = fourth;
		}

		public TFourth Fourth { get; set; }
	}

	public class RefFive<TFirst, TSecond, TThird, TFourth, TFifth> : RefQuadruple<TFirst, TSecond, TThird, TFourth>
	{
		public RefFive()
		{
		}

		public RefFive(TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth)
			: base(first, second, third, fourth)
		{
			Fifth = fifth;
		}

		public TFifth Fifth { get; set; }
	}

	public static class RefTuple
	{
		public static RefPair<TFirst, TSecond> Create<TFirst, TSecond>(TFirst first, TSecond second)
		{
			return new RefPair<TFirst, TSecond>(first, second);
		}

		public static RefTriple<TFirst, TSecond, TThird> Create<TFirst, TSecond, TThird>(TFirst first, TSecond second, TThird third)
		{
			return new RefTriple<TFirst, TSecond, TThird>(first, second, third);
		}

		public static RefQuadruple<TFirst, TSecond, TThird, TFourth> Create<TFirst, TSecond, TThird, TFourth>(TFirst first, TSecond second, TThird third, TFourth fourth)
		{
			return new RefQuadruple<TFirst, TSecond, TThird, TFourth>(first, second, third, fourth);
		}

		public static RefFive<TFirst, TSecond, TThird, TFourth, TFifth> Create<TFirst, TSecond, TThird, TFourth, TFifth>(TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth)
		{
			return new RefFive<TFirst, TSecond, TThird, TFourth, TFifth>(first, second, third, fourth, fifth);
		}
	}
}