namespace Ecng.Test.Common
{
	using System;
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
	}
}