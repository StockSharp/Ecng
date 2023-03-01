namespace Ecng.Tests.Common
{
	using System;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class NullableTest
	{
		[TestMethod]
		public void IsNull()
		{
			1.IsNull().AssertFalse();
			0.IsNull().AssertFalse();
			0.IsNull(true).AssertTrue();
			default(int).IsNull().AssertFalse();
			default(int?).IsNull().AssertTrue();
			default(int).IsNull(true).AssertTrue();
			default(int?).IsNull(true).AssertTrue();

			TimeSpan.Zero.IsNull().AssertFalse();
			TimeSpan.Zero.IsNull(true).AssertTrue();
			default(TimeSpan).IsNull().AssertFalse();
			default(TimeSpan?).IsNull().AssertTrue();
			default(TimeSpan).IsNull(true).AssertTrue();
			default(TimeSpan?).IsNull(true).AssertTrue();

			string.Empty.IsNull().AssertFalse();
			string.Empty.IsNull(true).AssertFalse();
			default(string).IsNull().AssertTrue();
			default(string).IsNull(true).AssertTrue();

			object o1 = 1;
			object o2 = 0;
			object o3 = default(int);
			object o4 = default(int?);

			o1.IsNull().AssertFalse();
			o1.IsNull(true).AssertFalse();
			o2.IsNull().AssertFalse();
			o2.IsNull(true).AssertTrue();
			o3.IsNull().AssertFalse();
			o3.IsNull(true).AssertTrue();
			o4.IsNull().AssertTrue();
			o4.IsNull(true).AssertTrue();

			o1 = TimeSpan.FromSeconds(1);
			o2 = TimeSpan.Zero;
			o3 = default(TimeSpan);
			o4 = default(TimeSpan?);

			o1.IsNull().AssertFalse();
			o1.IsNull(true).AssertFalse();
			o2.IsNull().AssertFalse();
			o2.IsNull(true).AssertTrue();
			o3.IsNull().AssertFalse();
			o3.IsNull(true).AssertTrue();
			o4.IsNull().AssertTrue();
			o4.IsNull(true).AssertTrue();

			o1 = "1";
			o2 = "";
			o3 = default(string);
			o4 = string.Empty;

			o1.IsNull().AssertFalse();
			o1.IsNull(true).AssertFalse();
			o2.IsNull().AssertFalse();
			o2.IsNull(true).AssertFalse();
			o3.IsNull().AssertTrue();
			o3.IsNull(true).AssertTrue();
			o4.IsNull().AssertFalse();
			o4.IsNull(true).AssertFalse();
		}
	}
}