namespace Ecng.Tests.Common
{
	using System;
	using System.Linq;
#if NET5_0_OR_GREATER
	using System.Runtime.CompilerServices;
#endif

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;
	using Ecng.UnitTesting;
	using Ecng.Serialization;

	[TestClass]
	public class TupleTests
	{
		private static void ValuesAndBack<T>(T tuple, bool isValue)
#if NET5_0_OR_GREATER
			where T : ITuple
#endif
		{
			tuple.AssertEqual(tuple.ToValues().ToTuple(isValue));
		}

		[TestMethod]
		public void ValuesAndBack()
		{
			ValuesAndBack(Tuple.Create(1, "123"), false);
			ValuesAndBack(Tuple.Create(1, 5.6, "123"), false);
			ValuesAndBack(Tuple.Create(1, DateTime.Now, "123"), false);
			ValuesAndBack(Tuple.Create(1, 1, 4, "123"), false);
			ValuesAndBack(Tuple.Create(1, 1, 4, (object)null, "123"), false);
		}

		[TestMethod]
		public void ValuesAndBack2()
		{
			ValuesAndBack((1, "123"), true);
			ValuesAndBack((1, 5.6, "123"), true);
			ValuesAndBack((1, DateTime.Now, "123"), true);
			ValuesAndBack((1, 1, 4, "123"), true);
			ValuesAndBack((1, 1, 4, (object)null, "123"), true);
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

		[TestMethod]
		public void ToStorage()
		{
			var p1 = RefTuple.Create(123, "123");
			var p2 = p1.ToStorage().ToRefPair<int, string>();
			p2.First.AssertEqual(p1.First);
			p2.Second.AssertEqual(p1.Second);
		}

		[TestMethod]
		public void ToValuesTupleTests()
		{
			var t = Tuple.Create(10, "10");
			var values = t.ToValues().ToArray();
			values.Length.AssertEqual(2);
			values[0].AssertEqual(t.Item1);
			values[1].AssertEqual(t.Item2);
		}

		[TestMethod]
		public void ToValuesValueTupleTests()
		{
			var t = ValueTuple.Create(10, "10");
			var values = t.ToValues().ToArray();
			values.Length.AssertEqual(2);
			values[0].AssertEqual(t.Item1);
			values[1].AssertEqual(t.Item2);
		}

		[TestMethod]
		public void ToValuesValTupleTests()
		{
			var t = (10, "10");
			var values = t.ToValues().ToArray();
			values.Length.AssertEqual(2);
			values[0].AssertEqual(t.Item1);
			values[1].AssertEqual(t.Item2);
		}

		//[TestMethod]
		//public void ToValuesRefTupleTests()
		//{
		//	var t = RefTuple.Create(10, "10");
		//	var values = t.ToValues().ToArray();
		//	values.Length.AssertEqual(2);
		//	values[0].AssertEqual(t.First);
		//	values[1].AssertEqual(t.Second);
		//}
	}
}