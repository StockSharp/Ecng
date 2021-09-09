namespace Ecng.Test.Serialization
{
	using System;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
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

		[TimeZoneInfo]
		public TimeZoneInfo TimeZone { get; set; }

		public TimeSpan TimeSpan { get; set; }

		public Guid Guid { get; set; }

		public Guid? NullGuid { get; set; }

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
				((Entities is null && other.Entities is null) || Entities?.SequenceEqual(other.Entities) == true) &&
				((Entities2 is null && other.Entities2 is null) || Entities2?.SequenceEqual(other.Entities2) == true) &&
				((Names is null && other.Names is null) || Names?.SequenceEqual(other.Names) == true) &&
				((Names2 is null && other.Names2 is null) || Names2?.SequenceEqual(other.Names2) == true) &&
				((TimeZone is null && other.TimeZone is null) || TimeZone?.Equals(other.TimeZone) == true) &&
				TimeSpan == other.TimeSpan &&
				Guid == other.Guid &&
				NullGuid == other.NullGuid
				;
		}
	}

	class TZEntity : Equatable<TZEntity>
	{
		public TimeZoneInfo TimeZone { get; set; }

		public override bool Equals(TZEntity other)
		{
			return TimeZone == other.TimeZone;
		}

		public override TZEntity Clone() => throw new NotSupportedException();
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
		public void TimeZoneBin()
		{
			Test<BinarySerializer<Entity>, Entity>(new Entity { TimeZone = TimeHelper.Moscow });
			Test<BinarySerializer<TZEntity>, TZEntity>(new TZEntity { TimeZone = TimeHelper.Moscow });
		}

		[TestMethod]
		public void TimeZoneXml()
		{
			Test<XmlSerializer<Entity>, Entity>(new Entity { TimeZone = TimeHelper.Moscow });
			Test<XmlSerializer<TZEntity>, TZEntity>(new TZEntity { TimeZone = TimeHelper.Moscow });
		}

		[TestMethod]
		public void TimeSpanBin()
		{
			Test<BinarySerializer<Entity>, Entity>(new Entity { TimeSpan = TimeSpan.FromDays(7) });
		}

		[TestMethod]
		public void TimeSpanXml()
		{
			Test<XmlSerializer<Entity>, Entity>(new Entity { TimeSpan = TimeSpan.FromDays(7) });
		}

		[TestMethod]
		public void GuidBin()
		{
			Test<BinarySerializer<Entity>, Entity>(new Entity { Guid = Guid.NewGuid() });
		}

		[TestMethod]
		public void GuidXml()
		{
			Test<XmlSerializer<Entity>, Entity>(new Entity { Guid = Guid.NewGuid() });
		}

		[TestMethod]
		public void NullGuidBin()
		{
			Test<BinarySerializer<Entity>, Entity>(new Entity { NullGuid = Guid.NewGuid() });
		}

		[TestMethod]
		public void NullGuidXml()
		{
			Test<XmlSerializer<Entity>, Entity>(new Entity { NullGuid = Guid.NewGuid() });
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

		[TestMethod]
		public void Range()
		{
			var r = new Range<int>(1, 10);
			r.ToStorage().ToRange<int>().AssertEqual(r);
		}

		[TestMethod]
		public void RefTuple()
		{
			var p1 = Ecng.Common.RefTuple.Create(123, "123");
			var p2 = p1.ToStorage().ToRefPair<int, string>();
			p2.First.AssertEqual(p1.First);
			p2.Second.AssertEqual(p1.Second);
		}
	}
}