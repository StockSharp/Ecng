namespace Ecng.Test.Data
{
	#region Using Directives

	using System;
	using System.Drawing;
	using System.Globalization;
	using System.Net;
	using System.Reflection;
	using System.Xml;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Data;
	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;
	using Ecng.Security;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	[Serializable]
	public abstract class FieldFactoryEntity<V>
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		[DefaultImp]
		public abstract long Id { get; set; }

		#endregion

		#region Value

		[DefaultImp]
		public abstract V Value { get; set; }

		#endregion
	}

	[Entity("SerializeTestEntity")]
	public abstract class SerializeBinaryTestEntity : FieldFactoryEntity<Image>
	{
		#region Value

		[BinaryFormatter]
		[DefaultImp]
		public override abstract Image Value { get; set; }

		#endregion
	}

	[Entity("SerializeTestEntity")]
	public abstract class SerializeXmlTestEntity : FieldFactoryEntity<Value>
	{
		#region Value

		[XmlFormatter]
		[DefaultImp]
		public override abstract Value Value { get; set; }

		#endregion
	}

	public abstract class ColorTestEntity : FieldFactoryEntity<Color>
	{
		#region Value

		[Color]
		[DefaultImp]
		public override abstract Color Value { get; set; }

		#endregion
	}

	public abstract class CryptoTestEntity : FieldFactoryEntity<string>
	{
		#region Value

		[Crypto]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("CryptoTestEntity")]
	public abstract class SymCryptoTestEntity2 : FieldFactoryEntity<string>
	{
		#region Value

		[Crypto(PublicKey = "/wSLfzApvDnYlBrGZV1zsichriJC+Eli1KgzdlIWAIQ=", KeyType = KeyTypes.Direct)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("CryptoTestEntity")]
	public abstract class SymCryptoTestEntity3 : FieldFactoryEntity<string>
	{
		#region Value

		[Crypto(PublicKey = @"..\..\..\EcngTest\Data\sym.key", KeyType = KeyTypes.File)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("CryptoTestEntity")]
	public abstract class AsymmetricCryptoTestEntity2 : FieldFactoryEntity<string>
	{
		#region Value

		[Crypto(PublicKey = "AwAAAAEAAYAAAACzCyWgndnbWzEtorHWL8F1RjL9HYs/xy/r+9RyTtONS7qcmE7cXzUb6CRjC1UHmXdl1pc1HBqugFT5ckKaGkS908RuxEFFEEPtROGneP46ua3PqFmD506xH5HJrxKq1nIEYpWXEzOdzhz2NYh9t8BI5ahyx+mTB2W6uw3Rq46Arw==", PrivateKey = "QAAAAOjjB+MO5qq/rsLXAv5owCz8+EYW1fSSDtblLe/4T9AClqEEvJfrq3IFdfiSCSip+s3Rs/5k2WIZ7VM9D0hMI3tAAAAAxNAd0ZHi4Ido7xC0nD6BymL+Vt1Q/SW+clzvtcHElHrxthTsv4eCfvTseNUFcG2rwn6xEsdsG2kNyjJ1k6PHXYAAAABn/+2XYpmNZWcnjv2l4I+LQ3+Sr3qXTWh0tw8sZsVTqc138LC+KT98OlgIgCigBXDpYsDqRKzq9/hj/Q7a3K9YavRXcBBUL2d916X+wzPZw2Pc01XJrY48Qdx9neArY34UH8eYzBp34AeZta+GEFT8Fn7TWHaVd6aSj0fIbkE74UAAAAArLcTNlXqxF98YIvNcJiHTdYe2vw8mTFpR/6X3wytRHtm8uvsYk8py1o5b6v+luXZV6NadiSdA6Bu3fi+yMOO3QAAAAE57h0iI2mYa2Vdr2/nqWytvqmjNPHyWTomgUd6y9EcZd8XaNkZyLTGfTaUpnU+mDDY0+zu31n5fuCYHVSeYyelAAAAAMuA2Mrbm20ZOkP5ZDYfD/aWez9YsqOFWnd6Prd4nBF/uOUrPJtd//WDjFsBYKp0/dTx+CIxZ+ixcVQfbxO7t5wMAAAABAAGAAAAAswsloJ3Z21sxLaKx1i/BdUYy/R2LP8cv6/vUck7TjUu6nJhO3F81G+gkYwtVB5l3ZdaXNRwaroBU+XJCmhpEvdPEbsRBRRBD7UThp3j+Ormtz6hZg+dOsR+Rya8SqtZyBGKVlxMznc4c9jWIfbfASOWocsfpkwdlursN0auOgK8=", KeyType = KeyTypes.Direct)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("CryptoTestEntity")]
	public abstract class AsymmetricCryptoTestEntity3 : FieldFactoryEntity<string>
	{
		#region Value

		[Crypto(PublicKey = @"..\..\..\EcngTest\Data\public.key", PrivateKey = @"..\..\..\EcngTest\Data\private.key", KeyType = KeyTypes.File)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("CryptoTestEntity")]
	public abstract class X509TestEntity : FieldFactoryEntity<string>
	{
		#region Value

		[X509(SerialNumber = "C7606BD486245683402D51E4DF6C0AA3")]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("CryptoTestEntity")]
	public abstract class X509TestEntity2 : FieldFactoryEntity<string>
	{
		#region Value

		[X509(FileName = @"..\..\..\EcngTest\Data\x509.cer")]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	public abstract class CultureTestEntity : FieldFactoryEntity<CultureInfo>
	{
		#region Value

		[Culture]
		[DefaultImp]
		public override abstract CultureInfo Value { get; set; }

		#endregion
	}

	public abstract class EnumTestEntity : FieldFactoryEntity<EnumTestEntity.TestEnum>
	{
		public enum TestEnum
		{
			Value1,
			Value2,
		}

		#region Value

		[Enum]
		[DefaultImp]
		public override abstract TestEnum Value { get; set; }

		#endregion
	}

	public abstract class IpAddressTestEntity : FieldFactoryEntity<IPAddress>
	{
		#region Value

		[IpAddress]
		[DefaultImp]
		public override abstract IPAddress Value { get; set; }

		#endregion
	}

	public class IPAddressOperator : BaseOperator<IPAddress>
	{
		#region Operator<IPAddress> Members

		public override IPAddress Add(IPAddress first, IPAddress second)
		{
			throw new NotSupportedException();
		}

		public override IPAddress Subtract(IPAddress first, IPAddress second)
		{
			throw new NotSupportedException();
		}

		public override IPAddress Multiply(IPAddress first, IPAddress second)
		{
			throw new NotSupportedException();
		}

		public override IPAddress Divide(IPAddress first, IPAddress second)
		{
			throw new NotSupportedException();
		}

		public override int Compare(IPAddress first, IPAddress second)
		{
			return first.Compare(second);
		}

		#endregion
	}

	public abstract class RangeTestEntity : FieldFactoryEntity<Range<long>>
	{
		#region Value

		//[Ecng.Serialization.FieldFactories.Range]
		[InnerSchema]
		[DefaultImp]
		public override abstract Range<long> Value { get; set; }

		#endregion
	}

	public abstract class MemberTestEntity : FieldFactoryEntity<MemberInfo>
	{
		#region Value

		[Member]
		[DefaultImp]
		public override abstract MemberInfo Value { get; set; }

		#endregion
	}

	public abstract class PasswordTestEntity : FieldFactoryEntity<Secret>
	{
		#region Value

		[InnerSchema]
		[DefaultImp]
		public override abstract Secret Value { get; set; }

		#endregion
	}

	public abstract class TimeSpanTestEntity : FieldFactoryEntity<TimeSpan>
	{
		#region Value

		[TimeSpan]
		[DefaultImp]
		public override abstract TimeSpan Value { get; set; }

		#endregion
	}

	[Entity("TrimTestEntity")]
	public abstract class TrimTestBothEntity : FieldFactoryEntity<string>
	{
		#region Value

		[Trim(Options = TrimOptions.Both)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("TrimTestEntity")]
	public abstract class TrimTestBeginEntity : FieldFactoryEntity<string>
	{
		#region Value

		[Trim(Options = TrimOptions.Start)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("TrimTestEntity")]
	public abstract class TrimTestEndEntity : FieldFactoryEntity<string>
	{
		#region Value

		[Trim(Options = TrimOptions.End)]
		[DefaultImp]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("MemberTestEntity")]
	public abstract class TypeTestEntity : FieldFactoryEntity<Type>
	{
		#region Value

		[Member]
		[DefaultImp]
		public override abstract Type Value { get; set; }

		#endregion
	}

	public abstract class UrlTestEntity : FieldFactoryEntity<Uri>
	{
		#region Value

		[Url]
		[DefaultImp]
		public override abstract Uri Value { get; set; }

		#endregion
	}

	public abstract class VersionTestEntity : FieldFactoryEntity<Version>
	{
		#region Value

		[Version]
		[DefaultImp]
		public override abstract Version Value { get; set; }

		#endregion
	}

	[Entity("XmlTestEntity")]
	public abstract class XmlDocTestEntity : FieldFactoryEntity<XmlDocument>
	{
		#region Value

		[Ecng.Serialization.Xml]
		[DefaultImp]
		public override abstract XmlDocument Value { get; set; }

		#endregion
	}

	[Entity("XmlTestEntity")]
	public abstract class XmlNodeTestEntity : FieldFactoryEntity<XmlNode>
	{
		#region Value

		[Ecng.Serialization.Xml]
		[DefaultImp]
		public override abstract XmlNode Value { get; set; }

		#endregion
	}

	[Entity("XmlTestEntity")]
	public abstract class XmlElemTestEntity : FieldFactoryEntity<XmlElement>
	{
		#region Value

		[Ecng.Serialization.Xml]
		[DefaultImp]
		public override abstract XmlElement Value { get; set; }

		#endregion
	}

	[Entity("XmlTestEntity")]
	public abstract class XmlAttrTestEntity : FieldFactoryEntity<System.Xml.XmlAttribute>
	{
		#region Value

		[Ecng.Serialization.Xml]
		[DefaultImp]
		public override abstract System.Xml.XmlAttribute Value { get; set; }

		#endregion
	}

	[TestClass]
	public class FieldFactoryTest
	{
		[TestMethod]
		public void SerializeBinary()
		{
			nTest<SerializeBinaryTestEntity, Image>(Properties.Resources.TestImage, arg =>
				Properties.Resources.TestImage.Compare(arg));
		}

		[TestMethod]
		public void SerializeXml()
		{
			nTest<SerializeXmlTestEntity, Value>(Config.Create<Value>());
		}

		[TestMethod]
		public void Color()
		{
			nTest<ColorTestEntity, Color>(System.Drawing.Color.Pink, arg =>
				System.Drawing.Color.Pink.Compare(arg));
		}

		[TestMethod]
		public void DpapiCrypto()
		{
			nTest<CryptoTestEntity, string>("John Smith");
		}

		[TestMethod]
		public void SymCryptoEnc()
		{
			nTest<SymCryptoTestEntity2, string>("John Smith");
		}

		[TestMethod]
		public void SymCryptoFile()
		{
			nTest<SymCryptoTestEntity3, string>("John Smith");
		}

		[TestMethod]
		public void AsymmetricCryptoEnc()
		{
			nTest<AsymmetricCryptoTestEntity2, string>("John Smith");
		}

		[TestMethod]
		public void AsymmetricCryptoFile()
		{
			nTest<AsymmetricCryptoTestEntity3, string>("John Smith");
		}

		[TestMethod]
		public void X509Store()
		{
			nTest<X509TestEntity, string>("John Smith");
		}

		[TestMethod]
		public void X509File()
		{
			nTest<X509TestEntity2, string>("John Smith");
		}

		[TestMethod]
		public void Culture()
		{
			nTest<CultureTestEntity, CultureInfo>(CultureInfo.CurrentCulture);
		}

		[TestMethod]
		public void Enum()
		{
			nTest<EnumTestEntity, EnumTestEntity.TestEnum>(EnumTestEntity.TestEnum.Value1);
		}

		[TestMethod]
		public void IpAddress()
		{
			nTest<IpAddressTestEntity, IPAddress>(IPAddress.Loopback);
		}

		[TestMethod]
		public void Range()
		{
			OperatorRegistry.AddOperator(new IPAddressOperator());
			nTest<RangeTestEntity, Range<long>>(new Range<long>(0, 10));
		}

		[TestMethod]
		public void Member()
		{
			nTest<MemberTestEntity, MemberInfo>(typeof(MemberTestEntity));
		}

		[TestMethod]
		public void Member2()
		{
			nTest<MemberTestEntity, MemberInfo>(typeof(MemberTestEntity).GetMember<PropertyInfo>("Id"));
		}

		[TestMethod]
		public void TrimTestBoth()
		{
			nTest<TrimTestBothEntity, string>("   John Smith ", "John Smith");
		}

		[TestMethod]
		public void TrimTestBegin()
		{
			nTest<TrimTestBeginEntity, string>("   John Smith ", "John Smith ");
		}

		[TestMethod]
		public void TrimTestEnd()
		{
			nTest<TrimTestEndEntity, string>("   John Smith ", "   John Smith");
		}

		[TestMethod]
		public void Type()
		{
			nTest<TypeTestEntity, Type>(typeof(TypeTestEntity));
		}

		[TestMethod]
		public void Url()
		{
			nTest<UrlTestEntity, Uri>(new Uri("http://msdn.microsoft.com"));
		}

		[TestMethod]
		public void Version()
		{
			nTest<VersionTestEntity, Version>(new Version(1, 4, 12043, 454322));
		}

		[TestMethod]
		public void XmlDoc()
		{
			var doc = new XmlDocument();
			doc.LoadXml("<doc><childNode></childNode></doc>");
			nTest<XmlDocTestEntity, XmlDocument>(doc, doc.Compare);
		}

		[TestMethod]
		public void XmlNode()
		{
			var doc = new XmlDocument();
			doc.LoadXml("<doc><childNode></childNode></doc>");
			nTest<XmlNodeTestEntity, XmlNode>(doc.DocumentElement, arg => doc.DocumentElement.Compare(arg));
		}

		[TestMethod]
		public void XmlNode2()
		{
			var doc = new XmlDocument();
			doc.LoadXml("<doc><childNode></childNode></doc>");
			nTest<XmlNodeTestEntity, XmlNode>(doc.SelectSingleNode("//doc/childNode"), arg => doc.SelectSingleNode("//doc/childNode").Compare(arg));
		}

		[TestMethod]
		public void XmlElem()
		{
			var doc = new XmlDocument();
			doc.LoadXml("<doc><childNode></childNode></doc>");
			nTest<XmlElemTestEntity, XmlElement>(doc.DocumentElement, arg => doc.DocumentElement.Compare(arg));
		}

		[TestMethod]
		public void XmlAttribute()
		{
			var doc = new XmlDocument();
			doc.LoadXml("<doc><childNode name='TestName1'></childNode></doc>");
			nTest<XmlAttrTestEntity, System.Xml.XmlAttribute>((System.Xml.XmlAttribute)doc.SelectSingleNode("//doc/childNode/@name"), arg =>
				doc.SelectSingleNode("//doc/childNode/@name").OuterXml == arg.OuterXml);
		}

		private static void nTest<T, V>(V value)
			where T : FieldFactoryEntity<V>
		{
			nTest<T, V>(value, value);
		}

		private static void nTest<T, V>(V value, V actualValue)
			where T : FieldFactoryEntity<V>
		{
			nTest<T, V>(value, arg => actualValue.Equals(arg));
		}

		private static void nTest<T, V>(V value, Predicate<V> predicate)
			where T : FieldFactoryEntity<V>
		{
			Config.CreateProxy<T>();

			using (Database db = Config.CreateDatabase())
			{
				var entity = Config.Create<T>();
				entity.Value = value;

				db.Create(entity);
				db.ClearCache();

				entity = db.Read<T>(entity.Id);

				Assert.IsTrue(predicate(entity.Value));

				db.Delete(entity);
			}
		}
	}
}