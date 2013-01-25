namespace Ecng.UnitTesting
{
	using System;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	public static class AssertHelper
	{
		public static void AssertTrue(this bool value)
		{
			Assert.IsTrue(value);
		}

		public static void AssertFalse(this bool value)
		{
			Assert.IsFalse(value);
		}

		public static void AssertNull(this object value)
		{
			Assert.IsNull(value);
		}

		public static void AssertNull(this Exception value)
		{
			if (value != null)
				throw value;
		}

		public static void AssertNotNull(this object value)
		{
			Assert.IsNotNull(value);
		}

		public static void AssertOfType<T>(this object value)
		{
			Assert.IsInstanceOfType(value, typeof(T));
		}

		public static void AssertNotOfType<T>(this object value)
		{
			Assert.IsNotInstanceOfType(value, typeof(T));
		}

		public static void AssertEqual<T>(this T value, T expected)
		{
			Assert.AreEqual(expected, value);
		}

		public static void AssertNotEqual<T>(this T value, T expected)
		{
			Assert.AreNotEqual(expected, value);
		}

		public static void AssertSame<T>(this T value, T expected)
		{
			Assert.AreSame(expected, value);
		}

		public static void AssertNotSame<T>(this T value, T expected)
		{
			Assert.AreNotSame(expected, value);
		}
	}
}