namespace Ecng.Tests.Common
{
	using System;
	using System.Linq;
#if NETSTANDARD2_1
	using System.Runtime.CompilerServices;
#endif

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;
	using Ecng.UnitTesting;

	[TestClass]
	public class TupleTests
	{
		private static void ValuesAndBack<T>(T tuple)
#if NETSTANDARD2_1
			where T : ITuple
#endif
		{
			tuple.AssertEqual(tuple.ToValues().ToTuple());
		}

		[TestMethod]
		public void ValuesAndBack()
		{
			ValuesAndBack(Tuple.Create(1, "123"));
			ValuesAndBack(Tuple.Create(1, 5.6, "123"));
			ValuesAndBack(Tuple.Create(1, DateTime.Now, "123"));
			ValuesAndBack(Tuple.Create(1, 1, 4, "123"));
			ValuesAndBack(Tuple.Create(1, 1, 4, (object)null, "123"));
		}

		[TestMethod]
		public void RefTupleNames()
		{
			var f = RefTuple.Create(123, "123", DateTime.Now, TimeSpan.FromSeconds(1), "345");

			RefTuple.GetName(0).AssertEqual(nameof(f.First));
			RefTuple.GetName(1).AssertEqual(nameof(f.Second));
			RefTuple.GetName(2).AssertEqual(nameof(f.Third));
			RefTuple.GetName(3).AssertEqual(nameof(f.Fourth));
			RefTuple.GetName(4).AssertEqual(nameof(f.Fifth));
		}

		[TestMethod]
		public void RefTupleValues()
		{
			var f = RefTuple.Create(123, "123", DateTime.Now, TimeSpan.FromSeconds(1), "345");

			var values = f.Values.ToArray();

			values[0].AssertEqual(f.First);
			values[1].AssertEqual(f.Second);
			values[2].AssertEqual(f.Third);
			values[3].AssertEqual(f.Fourth);
			values[4].AssertEqual(f.Fifth);
		}
	}
}