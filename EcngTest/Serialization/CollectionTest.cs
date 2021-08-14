namespace Ecng.Test.Serialization
{
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Serialization;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	/// <summary>
	/// Summary description for BinaryTest
	/// </summary>
	[TestClass]
	public class CollectionTest
	{
		[TestMethod]
		public void BinaryArray()
		{
			var array = new[] { "1", "2" };
			BinaryTest<string[], string>(array);
		}

		[TestMethod]
		public void XmlArray()
		{
			var array = new[] { "1", "2" };
			XmlTest<string[], string>(array);
		}

		[TestMethod]
		public void BinaryArrayNull()
		{
			var array = new[] { "1", null };
			BinaryTest<string[], string>(array);
		}

		[TestMethod]
		public void XmlArrayNull()
		{
			var array = new[] { "1", null };
			XmlTest<string[], string>(array);
		}

		[TestMethod]
		public void BinaryEnumerable()
		{
			var array = new[] { "1", "2" };
			BinaryTest<IEnumerable<string>, string>(array);
		}

		[TestMethod]
		public void XmlEnumerable()
		{
			var array = new[] { "1", "2" };
			XmlTest<IEnumerable<string>, string>(array);
		}

		[TestMethod]
		public void BinaryEnumerableNull()
		{
			var array = new[] { "1", null };
			BinaryTest<IEnumerable<string>, string>(array);
		}

		[TestMethod]
		public void XmlEnumerableNull()
		{
			var array = new[] { "1", null };
			XmlTest<IEnumerable<string>, string>(array);
		}

		[TestMethod]
		public void BinaryList()
		{
			var list = new List<string>(new[] { "1", "2" });
			BinaryTest<IList<string>, string>(list);
		}

		[TestMethod]
		public void XmlList()
		{
			var list = new List<string>(new[] { "1", "2" });
			XmlTest<IList<string>, string>(list);
		}

		[TestMethod]
		public void BinaryListNull()
		{
			var list = new List<string>(new[] { "1", null });
			BinaryTest<IList<string>, string>(list);
		}

		[TestMethod]
		public void XmlListNull()
		{
			var list = new List<string>(new[] { "1", null });
			XmlTest<IList<string>, string>(list);
		}

		[TestMethod]
		public void BinaryDictionaryPrimitive()
		{
			var dict = new Dictionary<int, string> { { 10, "10" }, { 100, "100" } };
			BinaryTest<IDictionary<int, string>, KeyValuePair<int, string>>(dict);
		}

		[TestMethod]
		public void XmlDictionaryPrimitive()
		{
			var dict = new Dictionary<int, string> { { 10, "10" }, { 100, "100" } };
			XmlTest<IDictionary<int, string>, KeyValuePair<int, string>>(dict);
		}

		[TestMethod]
		public void BinaryDictionaryComplex()
		{
			var dict = new Dictionary<Entity, DataAccessMethod> { { new Entity(), DataAccessMethod.Sequential }, { new Entity(), DataAccessMethod.Sequential } };
			BinaryTest<IDictionary<Entity, DataAccessMethod>, KeyValuePair<Entity, DataAccessMethod>>(dict);
		}

		[TestMethod]
		public void XmlDictionaryComplex()
		{
			var dict = new Dictionary<Entity, DataAccessMethod> { { new Entity(), DataAccessMethod.Sequential }, { new Entity(), DataAccessMethod.Sequential } };
			XmlTest<IDictionary<Entity, DataAccessMethod>, KeyValuePair<Entity, DataAccessMethod>>(dict);
		}

		//[TestMethod]
		//public void BinaryDictionaryDynamic()
		//{
		//	var dict = new Dictionary<object, object> { { new Entity(), DataAccessMethod.Sequential }, { new Entity2(), TestTimeout.Infinite } };
		//	BinaryTest<IDictionary<object, object>, KeyValuePair<object, object>>(dict);
		//}

		[TestMethod]
		public void XmlDictionaryDynamic()
		{
			var dict = new Dictionary<object, object> { { new Entity(), DataAccessMethod.Sequential }, { new Entity2(), TestTimeout.Infinite } };
			XmlTest<IDictionary<object, object>, KeyValuePair<object, object>>(dict);
		}

		[TestMethod]
		public void BinaryDictionaryNull()
		{
			var dict = new Dictionary<int, string> { { 10, "10" }, { 100, null } };
			BinaryTest<IDictionary<int, string>, KeyValuePair<int, string>>(dict);
		}

		[TestMethod]
		public void XmlDictionaryNull()
		{
			var dict = new Dictionary<int, string> { { 10, "10" }, { 100, null } };
			XmlTest<IDictionary<int, string>, KeyValuePair<int, string>>(dict);
		}

		private static void XmlTest<TCollection, TItem>(TCollection collection)
			where TCollection : IEnumerable<TItem>
		{
			Test<XmlSerializer<TCollection>, TCollection, TItem>(collection);
		}

		private static void BinaryTest<TCollection, TItem>(TCollection collection)
			where TCollection : IEnumerable<TItem>
		{
			Test<BinarySerializer<TCollection>, TCollection, TItem>(collection);
		}

		private static void Test<TSerializer, TCollection, TItem>(TCollection collection)
			where TSerializer : Serializer<TCollection>, new()
			where TCollection : IEnumerable<TItem>
		{
			var ser = new TSerializer();
			ser.Deserialize(ser.Serialize(collection)).SequenceEqual(collection).AssertTrue();
		}
	}
}
