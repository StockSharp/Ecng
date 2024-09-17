namespace Ecng.Common
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	public interface IRefTuple
	{
		IEnumerable<object> Values { get; set; }
	}

	public class RefPair<TFirst, TSecond> : IRefTuple
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

		public virtual IEnumerable<object> Values
		{
			get => [First, Second];
			set
			{
				First = (TFirst)value.ElementAt(0);
				Second = (TSecond)value.ElementAt(1);
			}
		}

		public override string ToString()
			=> "[" + GetValuesString() + "]";

		protected virtual string GetValuesString()
			=> First + ", " + Second;

		public KeyValuePair<TFirst, TSecond> ToValuePair()
			=> new(First, Second);
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

		public override IEnumerable<object> Values
		{
			get => base.Values.Concat([Third]);
			set
			{
				base.Values = value;
				Third = (TThird)value.ElementAt(2);
			}
		}

		protected override string GetValuesString()
			=> base.GetValuesString() + ", " + Third;
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

		public override IEnumerable<object> Values
		{
			get => base.Values.Concat([Fourth]);
			set
			{
				base.Values = value;
				Fourth = (TFourth)value.ElementAt(3);
			}
		}

		protected override string GetValuesString()
			=> base.GetValuesString() + ", " + Fourth;
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

		public override IEnumerable<object> Values
		{
			get => base.Values.Concat([Fifth]);
			set
			{
				base.Values = value;
				Fifth = (TFifth)value.ElementAt(4);
			}
		}

		protected override string GetValuesString()
			=> base.GetValuesString() + ", " + Fifth;
	}

	public static class RefTuple
	{
		public static RefPair<TFirst, TSecond> Create<TFirst, TSecond>(TFirst first, TSecond second)
			=> new(first, second);

		public static RefTriple<TFirst, TSecond, TThird> Create<TFirst, TSecond, TThird>(TFirst first, TSecond second, TThird third)
			=> new(first, second, third);

		public static RefQuadruple<TFirst, TSecond, TThird, TFourth> Create<TFirst, TSecond, TThird, TFourth>(TFirst first, TSecond second, TThird third, TFourth fourth)
			=> new(first, second, third, fourth);

		public static RefFive<TFirst, TSecond, TThird, TFourth, TFifth> Create<TFirst, TSecond, TThird, TFourth, TFifth>(TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth)
			=> new(first, second, third, fourth, fifth);

		private static readonly RefFive<int, int, int, int, int> _t = new();

		public static string GetName(int idx)
		{
			return idx switch
			{
				0 => nameof(_t.First),
				1 => nameof(_t.Second),
				2 => nameof(_t.Third),
				3 => nameof(_t.Fourth),
				4 => nameof(_t.Fifth),
				_ => throw new ArgumentOutOfRangeException(nameof(idx)),
			};
		}
	}
}