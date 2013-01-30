namespace Test
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Management;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.ServiceModel;
	using System.Threading;
	using System.Web.UI.WebControls;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Data;
	using Ecng.Data.Providers;
	using Ecng.Interop;
	using Ecng.Net;
	using Ecng.Reflection;
	using Ecng.Security;
	using Ecng.Serialization;
	using Ecng.UnitTesting;
	using Ecng.Xaml;

	using Microsoft.Practices.ServiceLocation;
	using Microsoft.Practices.Unity;
	using Microsoft.Practices.Unity.Configuration;

	using Wintellect.PowerCollections;

	class TestEntity
	{
		[Identity]
		public int Id { get; set; }

		[TimeSpan]
		public TimeSpan TimeSpan { get; set; }

		[InnerSchema(Order = 4)]
		[NameOverride("UglyName", "BeautyName")]
		[Ignore(FieldName = "IgnoreField")]
		public InnerEntity InnerEntity { get; set; }

		public InnerEntity2 InnerEntity2 { get; set; }
	}

	class InnerEntity
	{
		[Crypto(PublicKey = "/wSLfzApvDnYlBrGZV1zsichriJC+Eli1KgzdlIWAIQ=", KeyType = KeyTypes.Direct)]
		public string Password { get; set; }

		public string UglyName { get; set; }

		public int IgnoreField { get; set; }
	}

	struct InnerEntity2
	{
		public int Field1 { get; set; }
	}

	internal class MyClass : Equatable<MyClass>
	{
		public Range<TimeSpan> Range { get; set; }

		public override MyClass Clone()
		{
			throw new NotImplementedException();
		}
	}

	enum F1 : long
	{
		
	}

	class Program
	{
		static Range<TimeSpan> Method(Range<TimeSpan> range, TimeSpan value)
		{
			range.Min = value;
			return range;
		}

		static InnerEntity2 Method(InnerEntity2 range, int value)
		{
			range.Field1 = value;
			return range;
		}

		private static void Print(string table)
		{
			using (var mbs = new ManagementObjectSearcher("Select * From {0}".Put(table)))
			{
				using (var list = mbs.Get())
				{
					foreach (var obj in list.Cast<ManagementObject>())
					{
						foreach (var property in obj.Properties)
						{
							if (obj[property.Name] != null)
								Console.WriteLine("{0}.{1} = {2}", table, property.Name, obj[property.Name]);
						}
					}
				}
			}
		}

		[ServiceContract]
		public interface IInterface
		{
			[OperationContract]
			void Method1();
		}

		static void Main()
		{
			var root = new object();

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (Monitor.TryEnter(root))
						Monitor.Exit(root);
				}
			}));


			var syncObj = new SyncObject();

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (syncObj.TryEnter())
						syncObj.Exit();
				}
			}));
			return;

			var b23 = "true".To<bool?>();

			var storage = new SettingsStorage();
			storage.SetValue("1", 1);
			storage.SetValue("2", new[] { 1, 2 });
			storage.SetValue("3", new SettingsStorage());

			var ser = new XmlSerializer<SettingsStorage>();
			storage = ser.Deserialize(ser.Serialize(storage));

			foreach (var pair in storage)
			{
				Console.WriteLine("Name {0} Value {1}", pair.Key, pair.Value);
			}

			return;

			object i_val = new MyClass();
			object i_val2 = i_val;
			var count = 0;

			MyClass mc1 = new MyClass();
			MyClass mc2 = mc1;

			var gen = new ObjectIDGenerator();
			bool b;
			var id1 = gen.GetId(mc1, out b);
			var id2 = gen.GetId(mc2, out b);

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (i_val.Equals(i_val2))
						count++;
				}
			}));

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (mc1.Equals(mc2))
						count++;
				}
			}));

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (mc1 == mc2)
						count++;
				}
			}));

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (ReferenceEquals(mc1, mc2))
						count++;
				}
			}));

			Console.WriteLine(Watch.Do(() =>
			{
				for (int i = 0; i < 100000000; i++)
				{
					if (id1 == id2)
						count++;
				}
			}));

			return;

			//new DispatcherPropertyChangedEventManager(new GuiDispatcher());
			new SQLiteDatabaseProvider();
			return;

			"10,00".RemoveTrailingZeros();

			var moscowBaseUtcOffset = new TimeSpan(4, 0, 0);
			var zone = TimeZoneInfo.CreateCustomTimeZone("Московское время",
																   moscowBaseUtcOffset,
																   "Московское время",
																   "Московское время");
			Console.WriteLine(new NtpClient().GetLocalTime(zone));

			//Converter.GetAlias(typeof(Guid));
			//var b = typeof(Enum).IsEnum == typeof(Enum).IsEnum();

			//for (int i = 0; i < 10000; i++)
			//{
			//    var v = RandomGen.GetInt(-1000, 1000);
			//    var v2 = RandomGen.GetInt(1, 32);
			//    new BitArrayReader(v.To<byte[]>()).Read(v2).AssertEqual(new BitArrayReader(v.To<byte[]>()).OldRead(v2));

			//    var v3 = (long)RandomGen.GetInt(-1000, 1000);
			//    var v4 = RandomGen.GetInt(1, 64);
			//    new BitArrayReader(v3.To<byte[]>()).ReadLong(v4).AssertEqual(new BitArrayReader(v3.To<byte[]>()).OldReadLong(v4));
			//}

			//var str = typeof(string).AssemblyQualifiedName;

			//var v1 = 1.To<byte[]>();

			//var t1 = Watch.Do(() =>
			//{
			//    for (int i = 0; i < 10000000; i++)
			//    {
			//        new BitArrayReader(v1).Read(32);
			//    }
			//});

			//var dictTypes = new Dictionary<string, Type>
			//{
			//    { str.ToLowerInvariant(), typeof(string) }
			//};

			//var t2 = Watch.Do(() =>
			//{
			//    for (int i = 0; i < 10000000; i++)
			//    {
			//        new BitArrayReader(v1).OldRead(32);
			//    }
			//}););

			return;

			var md = new OrderedMultiDictionary<DateTime, int>(false);

			for (int i = 0; i < 24; i++)
			{
				md.Add(DateTime.Today + TimeSpan.FromHours(i), i);
			}

			foreach (var pair in md.Range(DateTime.Now, true, DateTime.Now, true))
			{
				Console.WriteLine(pair.Key + " - " + pair.Value.Select(i => i.ToString()).Join(","));
			}

			var db = ConfigManager.GetService<Database>();

			for (int i = 0; i < 1000; i++)
			{
				Console.WriteLine(RandomGen.GetBool());
			}
			return;
			//var invoker = FastInvoker<Range<TimeSpan>, TimeSpan, VoidType>.Create(typeof(Range<TimeSpan>).GetMember<PropertyInfo>("Min"), false);
			//var r = invoker.SetValue(new Range<TimeSpan>(), TimeSpan.MaxValue);

			var s2 = new BinarySerializer<Range<TimeSpan>>();
			s2.Deserialize(s2.Serialize(new Range<TimeSpan> { Min = TimeSpan.Zero, Max = TimeSpan.MaxValue }));
			//var s1 = SchemaManager.GetSchema<Range<TimeSpan>>();
			//typeof(Pair<string, int>).MakeByRefType();
			//var invoker = FastInvoker<Pair<string, int>, string, VoidType>.Create(typeof(Pair<string, int>).GetMember<FieldInfo>("First"), false);
			//var pair = new Pair<string, int>();
			//invoker.SetValue(pair, "ssss");

			IDictionary<object, object> dict = new Dictionary<object, object>
			{
				{ 222, "1111" },
				//{ 333, new TestEntity { Id = 10, TimeSpan = TimeSpan.FromSeconds(10) } },
				//{ "444", new TestEntity { Id = 10, TimeSpan = TimeSpan.FromSeconds(10), InnerEntity2 = new InnerEntity2 { Field1 = 10 } } },
			};

			var s = new BinarySerializer<IDictionary<object, object>>();
			s.Serialize(dict, "2.bin");
			var p = s.Deserialize(s.Serialize(dict));

			var schema = SchemaManager.GetSchema<TestEntity>();
			var serializer = new BinarySerializer<Schema>();
			serializer.Serialize(schema, "1.bin");

			serializer.Deserialize(serializer.Serialize(schema));

			var formatter = new BinaryFormatter();
			formatter.Serialize(new MemoryStream(), new NullableEx<TimeSpan>());

			double.MaxValue.AsRaw().AsRaw();
			float.MaxValue.AsRaw().AsRaw();
			var c = new UnityContainer().LoadConfiguration();
			//UnityConfigurationSection section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
			//section.Containers.Default.GetConfigCommand().Configure(c);
			//var database = new Database("Db1", "dfgghjhhjjh");
			//c.RegisterInstance(database);
			var db1 = c.Resolve<Database>();
			var locator = new UnityServiceLocator(c);
			ServiceLocator.SetLocatorProvider(() => locator);
			var db2 = ServiceLocator.Current.GetInstance<Database>();

			Console.WriteLine(object.ReferenceEquals(db1, db2));
		}
	}
}