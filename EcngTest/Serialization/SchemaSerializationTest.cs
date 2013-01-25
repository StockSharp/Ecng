namespace Ecng.Test.Serialization
{
	using System;
	using System.IO;

	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	class TestEntity
	{
		[Identity]
		public int Id { get; set; }

		[TimeSpan]
		public TimeSpan TimeSpan { get; set; }

		[InnerSchema(Order = 4)]
		[NameOverride("UglyName", "BeautyName")]
		[Ecng.Serialization.Ignore(FieldName = "IgnoreField")]
		public InnerEntity InnerEntity { get; set; }

		public InnerEntity2 InnerEntity2 { get; set; }
	}

	[EntityFactory(typeof(UnitializedEntityFactory<InnerEntity>))]
	class InnerEntity
	{
		[Crypto(PublicKey = "/wSLfzApvDnYlBrGZV1zsichriJC+Eli1KgzdlIWAIQ=", KeyType = KeyTypes.Direct)]
		public string Password { get; set; }

		[Stream(Order = 0)]
		[Primitive(Order = 1)]
		public Stream UglyName { get; set; }

		public int IgnoreField { get; set; }
	}

	struct InnerEntity2
	{
		public int Field1 { get; set; }
	}

	[TestClass]
	public class SchemaSerializationTest
	{
		[TestMethod]
		public void Xml()
		{
			Test<XmlSerializer<Schema>>();
		}

		[TestMethod]
		public void Binary()
		{
			Test<BinarySerializer<Schema>>();
		}

		private static void Test<TSerializer>()
			where TSerializer : Serializer<Schema>, new()
		{
			var schema = SchemaManager.GetSchema<TestEntity>();

			var serializer = new TSerializer();
			var clone = serializer.Deserialize(serializer.Serialize(SchemaManager.GetSchema<TestEntity>()));

			Assert.AreEqual(schema, clone);
			Assert.AreEqual(schema.Fields.Count, clone.Fields.Count);

			for (var i = 0; i < schema.Fields.Count; i++)
			{
				Assert.AreEqual(schema.Fields[i], clone.Fields[i]);
			}
		}
	}
}