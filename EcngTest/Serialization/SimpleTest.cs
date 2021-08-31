namespace Ecng.Test.Serialization
{
	using System;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	class Entity2 : Equatable<Entity2>
	{
		public string Name { get; set; }

		public override Entity2 Clone() => throw new NotSupportedException();

		public override bool Equals(Entity2 other)
		{
			return Name == other.Name;
		}
	}

	class Entity : Equatable<Entity>
	{
		public int Id { get; set; }
		
		public string Name { get; set; }
		
		[InnerSchema]
		public Entity2 Entity2 { get; set; }
		
		[Collection]
		public Entity2[] Entities { get; set; }
		
		[Collection]
		public Entity2[] Entities2 { get; set; }
		
		[Collection]
		public string[] Names { get; set; }

		[Collection]
		public string[] Names2 { get; set; }

		public override Entity Clone() => throw new NotSupportedException();

		public override bool Equals(Entity other)
		{
			return
				Id == other.Id &&
				Name == other.Name &&
				Entity2 == other.Entity2 &&
				((Entities is null && other.Entities is null) || Entities.SequenceEqual(other.Entities)) &&
				((Entities2 is null && other.Entities2 is null) || Entities2.SequenceEqual(other.Entities2)) &&
				((Names is null && other.Names is null) || Names.SequenceEqual(other.Names)) &&
				((Names2 is null && other.Names2 is null) || Names2.SequenceEqual(other.Names2))
				;
		}
	}

	[TestClass]
	public class SimpleTest
	{
		[TestMethod]
		public void NullValues()
		{
			var ser = new BinarySerializer<Entity>();
			ser.Deserialize(ser.Serialize(new Entity { Name = "11", Entities2 = new[] { new Entity2 { Name = "22" }, null }, Names2 = new[] { "", null } }));
		}

		[TestMethod]
		public void BinaryIntTest()
		{
			Test<BinarySerializer<int>, int>(10);
		}

		[TestMethod]
		public void XmlByteTest()
		{
			Test<XmlSerializer<byte>, byte>(10);
		}

		[TestMethod]
		public void BinaryEnumTest()
		{
			Test<BinarySerializer<VisibleScopes>, VisibleScopes>(VisibleScopes.Public);
		}

		[TestMethod]
		public void XmlEnumTest()
		{
			Test<XmlSerializer<VisibleScopes>, VisibleScopes>(VisibleScopes.Public);
		}

		[TestMethod]
		public void BinaryStringTest()
		{
			Test<BinarySerializer<string>, string>("20");
		}

		[TestMethod]
		public void XmlStringTest()
		{
			Test<XmlSerializer<string>, string>("20");
		}

		[TestMethod]
		public void BinaryStringNullTest()
		{
			Test<BinarySerializer<string>, string>(null);
		}

		[TestMethod]
		public void XmlStringNullTest()
		{
			Test<XmlSerializer<string>, string>(null);
		}

		[TestMethod]
		public void BinaryDateTimeTest()
		{
			Test<BinarySerializer<DateTime>, DateTime>(DateTime.Now);
		}

		[TestMethod]
		public void XmlDateTimeTest()
		{
			Test<XmlSerializer<DateTime>, DateTime>(DateTime.Now);
		}

		[TestMethod]
		public void BinaryTimeSpanTest()
		{
			Test<BinarySerializer<TimeSpan>, TimeSpan>(DateTime.Now.TimeOfDay);
		}

		[TestMethod]
		public void XmlTimeSpanTest()
		{
			Test<XmlSerializer<TimeSpan>, TimeSpan>(DateTime.Now.TimeOfDay);
		}

		[TestMethod]
		public void BinaryTimeSpanNullTest()
		{
			Test<BinarySerializer<TimeSpan?>, TimeSpan?>(DateTime.Now.TimeOfDay);
		}

		[TestMethod]
		public void XmlTimeSpanNullTest()
		{
			Test<XmlSerializer<TimeSpan?>, TimeSpan?>(DateTime.Now.TimeOfDay);
		}

		[TestMethod]
		public void BinaryTimeSpanNullTest2()
		{
			Test<BinarySerializer<TimeSpan?>, TimeSpan?>(null);
		}

		[TestMethod]
		public void XmlTimeSpanNullTest2()
		{
			Test<XmlSerializer<TimeSpan?>, TimeSpan?>(null);
		}

		private static void Test<TSerializer, TValue>(TValue value)
			where TSerializer : Serializer<TValue>, new()
		{
			var serializer = new TSerializer();
			serializer.Deserialize(serializer.Serialize(value)).AssertEqual(value);
		}

		[TestMethod]
		public void BinarySecureStringTest()
		{
			Test<BinarySerializer<SecureString>, SecureString>("20".Secure());
		}

		[TestMethod]
		public void XmlSecureStringTest()
		{
			Test<XmlSerializer<SecureString>, SecureString>("20".Secure());
		}

		[TestMethod]
		public void BinarySecureStringNullTest()
		{
			Test<BinarySerializer<SecureString>, SecureString>(null);
		}

		[TestMethod]
		public void XmlSecureStringNullTest()
		{
			Test<XmlSerializer<SecureString>, SecureString>(null);
		}
	}
}