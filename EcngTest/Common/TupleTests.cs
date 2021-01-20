namespace Ecng.Test.Common
{
	using System;
	using System.Runtime.CompilerServices;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;
	using Ecng.UnitTesting;

	[TestClass]
	public class TupleTests
	{
		private static void ValuesAndBack(ITuple tuple)
		{
			throw new NotImplementedException();
			//tuple.AssertEqual(tuple.ToValues().ToTuple());
		}

		[TestMethod]
		public void ValuesAndBack()
		{
			ValuesAndBack(Tuple.Create(1, "123"));
			ValuesAndBack(Tuple.Create(1, 5.6, "123"));
			ValuesAndBack(Tuple.Create(1, DateTime.Now, "123"));
			ValuesAndBack(Tuple.Create(1, 1, 4, "123"));
		}
	}
}