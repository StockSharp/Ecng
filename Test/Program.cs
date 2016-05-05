namespace Test
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Drawing;
	using System.Drawing.Drawing2D;
	using System.Drawing.Imaging;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Management;
	using System.Net;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.ServiceModel;
	using System.Text;
	using System.Threading;
	using System.Web.UI.WebControls;
	using System.Windows.Media;

	using Ecng.Backup;
	using Ecng.Backup.Yandex;
	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Data;
	using Ecng.Data.Providers;
	using Ecng.Interop;
	using Ecng.Net;
	using Ecng.Reflection;
	using Ecng.Roslyn;
	using Ecng.Security;
	using Ecng.Serialization;
	using Ecng.UnitTesting;
	using Ecng.Xaml;

	using Microsoft.Practices.ServiceLocation;
	using Microsoft.Practices.Unity;
	using Microsoft.Practices.Unity.Configuration;

	using Wintellect.PowerCollections;

	using Rectangle = System.Windows.Shapes.Rectangle;

	public interface ICalc
	{
		decimal Calc(decimal[] prices);
	}

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
		[Value("")]
		F,
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

		internal enum TimeInForce
		{
			PutInQueue,
			CancelBalance,
			MatchOrCancel
		}

		internal enum OrderTypes
		{
			Execute,
			ExtRepo,
			Limit,
			Market,
		}

		internal enum CurrencyTypes
		{
			MGF,
			JPY,
			XCD,
			GTQ,
			BND,
			EEK,
			CZK,
			TJS,
			BBD,
			SKK,
			SDD,
			XPT,
			KES,
			KHR,
			KPW,
		}

		internal enum OrderStatus
		{
			NotValidated,
			CanceledByManager,
			NotAcceptedByManager,
			SentToCanceled,
			Accepted,
			GateError,
			RejectedBySystem,
			NotSigned,
			NotSupported,
		}

		internal enum OrderStates
		{
			Active,
			Pending,
			Failed,
			None,
		}

		internal enum Sides
		{
			Buy,
			Sell
		}

		internal enum ExecutionTypes
		{
		}

		/// <summary>
		/// <see cref="DateTime"/> format.
		/// </summary>
		public const string DateFormat = "yyyyMMdd";

		/// <summary>
		/// <see cref="TimeSpan"/> format.
		/// </summary>
		public const string TimeFormat = "HHmmssfff";

		internal class ExecutionMessage
		{
			public long TransactionId { get; set; }
			public long OriginalTransactionId { get; set; }
			public long? OrderId { get; set; }
			public string OrderStringId { get; set; }
			public string OrderBoardId { get; set; }
			public string UserOrderId { get; set; }
			public decimal OrderPrice { get; set; }
			public decimal? OrderVolume { get; set; }
			public decimal? Balance { get; set; }
			public decimal? VisibleVolume { get; set; }
			public Sides Side { get; set; }
			public Sides? OriginSide { get; set; }
			public OrderStates? OrderState { get; set; }
			public long? TradeId { get; set; }
			public string TradeStringId { get; set; }
			public decimal? TradePrice { get; set; }
			public decimal? TradeVolume { get; set; }
			public string PortfolioName { get; set; }
			public string ClientCode { get; set; }
			public string BrokerCode { get; set; }
			public string DepoName { get; set; }
			public bool? IsSystem { get; set; }
			public bool HasOrderInfo { get; set; }
			public bool HasTradeInfo { get; set; }
			public decimal? Commission { get; set; }
			public string Comment { get; set; }
			public string SystemComment { get; set; }
			public long? DerivedOrderId { get; set; }
			public string DerivedOrderStringId { get; set; }
			public bool? IsUpTick { get; set; }
			public bool IsCancelled { get; set; }
			public decimal? OpenInterest { get; set; }
			public decimal? PnL { get; set; }
			public decimal? Position { get; set; }
			public decimal? Slippage { get; set; }
			public int? TradeStatus { get; set; }
			public TimeSpan? Latency { get; set; }
			public OrderTypes? OrderType { get; set; }
			public TimeInForce? TimeInForce { get; set; }
			public CurrencyTypes? Currency { get; set; }
			public OrderStatus? OrderStatus { get; set; }
			public DateTimeOffset? ExpiryDate { get; set; }
			public InvalidOperationException Error { get; set; }
			public DateTimeOffset ServerTime { get; set; }
		}

		public static DateTimeOffset ReadTime(FastCsvReader reader, DateTime date)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			return (date + reader.ReadDateTime(TimeFormat).TimeOfDay).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Replace("+", string.Empty)));
		}

		const int _iterCount = 1000000;
		const string _dtTemplate = "yyyyMMdd HH:mm:ss fff";
		const string _tsTemplate = "hh\\:mm\\:ss\\ fff";

		class Class1
		{
			public static implicit operator double(Class1 d)
			{
				return 1;
			}
		}

		[STAThread]
		static void Main()
		{
			Console.WriteLine(new Version(4, 3, 14, 0).Compare(new Version(4, 3, 14, 1)));

			return;
			var compiler = Compiler.Create(CompilationLanguages.CSharp);
			var res = compiler.Compile("test", @"
//using System;
using Test;

class TestCalc : ICalc
{
	decimal ICalc.Calc(decimal[] prices)
	{
		return prices[0] * prices[1];
	}
}
", new[] { typeof(object).Assembly.Location, Assembly.GetExecutingAssembly().Location });

			foreach (var error in res.Errors)
			{
				Console.WriteLine(error.Message);
			}

			if (res.Assembly != null)
			{
				var t = res.Assembly.GetType("TestCalc");
				var calc = t.CreateInstance<ICalc>();
				Console.WriteLine(calc.Calc(new decimal[] { 6, 4 }));
			}

			return;
			TimeSpan elapsed;

			//using (IBackupService disk = new YandexDiskService())
			//{
			//	disk.Init();

			//	foreach (var entry in disk.Get(null))
			//	{
			//		if (entry.Name.CompareIgnoreCase("stocksharp"))
			//		{
			//			Console.WriteLine(entry);

			//			foreach (var entry1 in disk.Get(entry))
			//			{
			//				var stream = new MemoryStream();
			//				disk.Download(entry1, stream, p1 => { });

			//				Console.WriteLine("{0}={1}, url={2}", entry1.Name, stream.Length, disk.Publish(entry1));
			//				disk.UnPublish(entry1);
			//			}
			//		}
			//	}
			//}

			//return;

			var obj1 = new Class1();
			obj1.To<double>();

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					obj1.To<double>();
				}
			});

			Console.WriteLine("To: " + elapsed.TotalMilliseconds);

			Converter.AddTypedConverter<Class1, double>(cl => cl);

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					obj1.To<double>();
				}
			});
			
			Console.WriteLine("TypedTo: " + elapsed.TotalMilliseconds);
			return;

			var typedConverter = Converter.GetTypedConverter<string, IPAddress>();

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					typedConverter("127.0.0.1");
				}
			});

			Console.WriteLine("Typed converter: " + elapsed.TotalMilliseconds);

			var dateTimeString = DateTime.Now.ToString(_dtTemplate);

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					DateTime.ParseExact(dateTimeString, _dtTemplate, null);
				}
			});



			Console.WriteLine("DateTime.Parse: " + elapsed.TotalMilliseconds);

			var dtParser = new FastDateTimeParser(_dtTemplate);

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					dtParser.Parse(dateTimeString);
				}
			});

			Console.WriteLine("DateTimeParser.Parse: " + elapsed.TotalMilliseconds);

			var timeSpanString = DateTime.Now.TimeOfDay.ToString(_tsTemplate);

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					TimeSpan.ParseExact(timeSpanString, _tsTemplate, null);
				}
			});

			Console.WriteLine("TimeSpan.Parse: " + elapsed.TotalMilliseconds);

			var tsParser = new FastTimeSpanParser(_tsTemplate);

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					tsParser.Parse(timeSpanString);
				}
			});

			Console.WriteLine("DateTimeParser.Parse: " + elapsed.TotalMilliseconds);

			elapsed = Watch.Do(() =>
			{
				for (int i = 0; i < _iterCount; i++)
				{
					var hours = (timeSpanString[0] - '0') * 10 + (timeSpanString[1] - '0');
					var minutes = (timeSpanString[3] - '0') * 10 + (timeSpanString[4] - '0');
					var seconds = (timeSpanString[6] - '0') * 10 + (timeSpanString[7] - '0');
					var milli = (timeSpanString[9] - '0') * 100 + (timeSpanString[10] - '0') * 10 + (timeSpanString[11] - '0');

					new TimeSpan(0, hours, minutes, seconds, milli);
				}
			});

			Console.WriteLine("Manual: " + elapsed.TotalMilliseconds);

			return;
			Console.WriteLine(new XmlSerializer<Type[]>().Deserialize(new XmlSerializer<Type[]>().Serialize(new[] { typeof(int) }))[0]);
			var body = File.ReadAllBytes("csharp.jpg").To<Stream>();
			var srcImage = new Bitmap(body);

			var coeff = (double)srcImage.Width / 500;

			if (coeff <= 1)
				coeff = (double)srcImage.Height / 500;

			var newWidth = (int)(srcImage.Width / coeff);
			var newHeight = (int)(srcImage.Height / coeff);

			Bitmap newImage = new Bitmap(newWidth, newHeight);
			using (Graphics gr = Graphics.FromImage(newImage))
			{
				gr.SmoothingMode = SmoothingMode.HighQuality;
				gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
				gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
				gr.DrawImage(srcImage, new System.Drawing.Rectangle(0, 0, newWidth, newHeight));
			}

			body = new MemoryStream();
			newImage.Save(body, ImageFormat.Png);

			File.WriteAllBytes("csharp2.png", body.To<byte[]>());

			Console.WriteLine("{0}x{1}", newImage.Width, newImage.Height);

			return;

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