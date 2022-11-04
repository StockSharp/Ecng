namespace Test
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Backup;
	using Ecng.Backup.Amazon;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.ComponentModel.Expressions;
	using Ecng.Net;
	using Ecng.Reflection;
	using Ecng.Compilation.Roslyn;
	using Ecng.Serialization;
	using Ecng.UnitTesting;
	using Ecng.Collections;
	using Ecng.Security;
	using LibGit2Sharp;
	using System.Security;
	// using LinqToDB.Data;
	// using LinqToDB.DataProvider.SQLite;
	// using LinqToDB.DataProvider.SqlServer;
	// using LinqToDB;
	//using Microsoft.Data.SqlClient;
	using System.Net.Http.Formatting;
	using System.Net.Http;
	using System.Diagnostics;
	using System.Net;
	using Nito.AsyncEx;
	using Ecng.Reflection.Emit;
	using Newtonsoft.Json;
	using System.Reflection;

	//using ISO._4217;

	public interface ICalc
	{
		decimal Calc(decimal[] prices);
	}

	//class TestEntity
	//{
	//	[Identity]
	//	public int Id { get; set; }

	//	//[TimeSpan]
	//	public TimeSpan TimeSpan { get; set; }

	//	[InnerSchema(Order = 4)]
	//	[NameOverride("UglyName", "BeautyName")]
	//	[Ignore(FieldName = "IgnoreField")]
	//	public InnerEntity InnerEntity { get; set; }

	//	public InnerEntity2 InnerEntity2 { get; set; }
	//}

	class InnerEntity
	{
		//[Crypto(PublicKey = "/wSLfzApvDnYlBrGZV1zsichriJC+Eli1KgzdlIWAIQ=", KeyType = KeyTypes.Direct)]
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
		//[Value("")]
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

		//private static void Print(string table)
		//{
		//	using (var mbs = new ManagementObjectSearcher("Select * From {0}".Put(table)))
		//	{
		//		using (var list = mbs.Get())
		//		{
		//			foreach (var obj in list.Cast<ManagementObject>())
		//			{
		//				foreach (var property in obj.Properties)
		//				{
		//					if (obj[property.Name] != null)
		//						Console.WriteLine("{0}.{1} = {2}", table, property.Name, obj[property.Name]);
		//				}
		//			}
		//		}
		//	}
		//}

		//[ServiceContract]
		public interface IInterface
		{
			//[OperationContract]
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
			if (reader is null)
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

		private static void EnsureClean(Repository repository)
		{
			var statusOptions = new StatusOptions
			{
				IncludeIgnored = false,
				RecurseUntrackedDirs = false,
				IncludeUntracked = false
			};

			var status = repository.RetrieveStatus(statusOptions).ToList();

			if (!status.Any())
				return;

			var sb = new StringBuilder();

			sb.AppendLine($"({repository.Info.Path}) git repo is dirty:");
			foreach (var item in status)
				sb.AppendLine($"{item.State}: {item.FilePath}");

			throw new InvalidOperationException(sb.ToString());
		}

		private static void SyncRepo(Repository repository, string username, string password)
		{
			var signature = new Signature("invalid signature. not used for fast forward merge.", "invalidemail@invalidemail.net", DateTimeOffset.Now);
			var pullOptions = new PullOptions
			{
				MergeOptions = new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly }
			};

			if (username?.Length > 0 || password?.Length > 0)
				pullOptions.FetchOptions = new FetchOptions
				{
					CredentialsProvider = (url, usernameFromUrl, types) => new UsernamePasswordCredentials
					{
						Username = username,
						Password = password
					}
				};

			var result = Commands.Pull(repository, signature, pullOptions);

			if (result.Status != MergeStatus.FastForward && result.Status != MergeStatus.UpToDate)
				throw new InvalidOperationException($"unexpected pull status {result.Status}");
		}

		private class TestJsonObj : Equatable<TestJsonObj>, IPersistable
		{
			public int IntProp { get; set; }
			public DateTime DateProp { get; set; }
			public TimeSpan TimeProp { get; set; }
			public TestJson2Obj Obj1 { get; set; }
			public TestJson2Obj[] Obj2 { get; set; }

			public override TestJsonObj Clone()
			{
				return PersistableHelper.Clone(this);
			}

			public override bool Equals(TestJsonObj other)
			{
				return
					IntProp == other.IntProp &&
					DateProp == other.DateProp &&
					TimeProp == other.TimeProp &&
					Obj1 == other.Obj1 &&
					((Obj2 is null && other.Obj2 is null) || Obj2.SequenceEqual(other.Obj2))
					;
			}

			void IPersistable.Load(SettingsStorage storage)
			{
				IntProp = storage.GetValue<int>(nameof(IntProp));
				DateProp = storage.GetValue<DateTime>(nameof(DateProp));
				TimeProp = storage.GetValue<TimeSpan>(nameof(TimeProp));

				//if (storage.ContainsKey(nameof(Obj1)))
				Obj1 = storage.GetValue<SettingsStorage>(nameof(Obj1)).Load<TestJson2Obj>();

				//if (storage.ContainsKey(nameof(Obj2)))
				Obj2 = storage.GetValue<SettingsStorage[]>(nameof(Obj2))?.Select(s => s?.Load<TestJson2Obj>()).ToArray();
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				storage
					.Set(nameof(IntProp), IntProp)
					.Set(nameof(DateProp), DateProp)
					.Set(nameof(TimeProp), TimeProp)
					.Set(nameof(Obj1), Obj1?.Save())
					.Set(nameof(Obj2), Obj2?.Select(o => o?.Save()));
			}
		}

		private class TestJson2Obj : Equatable<TestJson2Obj>, IPersistable
		{
			public int IntProp { get; set; }
			public DateTime DateProp { get; set; }
			public TimeSpan TimeProp { get; set; }

			public override TestJson2Obj Clone()
			{
				return PersistableHelper.Clone(this);
			}

			public override bool Equals(TestJson2Obj other)
			{
				return
					IntProp == other.IntProp &&
					DateProp == other.DateProp &&
					TimeProp == other.TimeProp
					;
			}

			void IPersistable.Load(SettingsStorage storage)
			{
				IntProp = storage.GetValue<int>(nameof(IntProp));
				DateProp = storage.GetValue<DateTime>(nameof(DateProp));
				TimeProp = storage.GetValue<TimeSpan>(nameof(TimeProp));
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				storage
					.Set(nameof(IntProp), IntProp)
					.Set(nameof(DateProp), DateProp)
					.Set(nameof(TimeProp), TimeProp);
			}
		}

		private static async Task Do<T>(T value, CancellationToken token = default)
		{
			var ser = new JsonSerializer<T> { FillMode = false };
			var stream = new MemoryStream();
			await ser.SerializeAsync(value, stream, token);
			stream.Position = 0;

			Console.WriteLine(stream.To<byte[]>().UTF8());

			var needCast = value is SettingsStorage;

			var actual = await ser.DeserializeAsync(stream, token);

			void CheckValue(object actual, object expected)
			{
				void AssertEqual(object actual, object expected)
				{
					if (expected is null)
						actual.AssertNull();
					else
					{
						actual.AssertNotNull();

						if (!needCast)
							actual.AssertEqual(expected);
						else
						{
							var expType = expected.GetType();

							if (expType.GetGenericType(typeof(KeyValuePair<,>)) != null)
							{
								dynamic expDyn = expected;
								dynamic actDyn = actual;

								((string)actDyn.Key).AssertEqual((string)expDyn.Key);

								CheckValue((object)actDyn.Value, (object)expDyn.Value);
							}
							else
								actual.To(expType).AssertEqual(expected);
						}
					}
				}

				if (expected is IEnumerable expEmu)
				{
					actual.AssertNotNull();

					var actEmu = (IEnumerable)actual;
					//actCol.Count.AssertEqual(expCol.Count);

					var enumerator = expEmu.GetEnumerator();
					foreach (var actItem in actEmu)
					{
						if (!enumerator.MoveNext())
							throw new InvalidOperationException("Exp is less.");

						var expItem = enumerator.Current;

						if (expItem is IEnumerable)
							CheckValue(actItem, expItem);
						else
							AssertEqual(actItem, expItem);
					}

					if (enumerator.MoveNext())
						throw new InvalidOperationException("Exp is more.");
				}
				else
					AssertEqual(actual, expected);
			}

			CheckValue(actual, value);
		}

		public class InvokeMethod
		{
			public void VoidMethodWithParams5(params object[] args)
			{
				(args.Length == 2).AssertTrue();
				args[0].AreEqual("Mark Twain");
				args[1].AreEqual("John Smith");
			}

			public void VoidMethodWithParams8(string str, params int[] args)
			{
				str.AreEqual("Mark Twain");
				(args.Length == 2).AssertTrue();
				args[0].AreEqual(1);
				args[1].AreEqual(10);
			}
		}

		public static void Invoke8(InvokeMethod instance, object[] args)
		{
			var offset = 7;
			var arr2 = new int[args.Length - offset];
			Array.Copy(args, offset, arr2, 0, arr2.Length);
			instance.VoidMethodWithParams8((string)args[0], arr2);
		}

		private static void Temp<T>(T value)
		{
			Console.WriteLine(value is SecureString);
		}

		private class TestTable
		{
			public long Id { get; set; }
			public string Field1 { get; set; }
		}

		private class SberRestRegisterResponse
		{
			public string OrderId { get; set; }
			public string FormUrl { get; set; }
			public string ErrorCode { get; set; }
			public string ErrorMessage { get; set; }
			public string ExternalParams { get; set; }
		}

		private class SberRestClient : RestBaseApiClient
		{
			public SberRestClient(HttpClient client)
				: base(client, new JsonMediaTypeFormatter { Indent = true }, new JsonMediaTypeFormatter())
			{
				BaseAddress = new Uri("https://3dsec.sberbank.ru".To<Uri>(), "payment/rest/");
			}

			protected override string FormatRequestUri(string requestUri)
				=> $"{requestUri[0].ToLower()}{requestUri.Substring(1).Remove("Async")}.do";

			public Task<SberRestRegisterResponse> RegisterAsync(string token, string orderNumber, string amount, string currency, string returnUrl, string failUrl, string description, string language, string expirationDate, string features, int skip = default, CancellationToken cancellationToken = default)
				=> GetAsync<SberRestRegisterResponse>(GetCurrentMethod(), cancellationToken, token, orderNumber, amount, currency, returnUrl, failUrl, description, language, expirationDate, features, skip);
		}

		//private static readonly int _connections = 10;
		//private static readonly HttpClient _httpClient = new HttpClient();

		//private static void TestHttpClientWithUsing()
		//{
		//	try
		//	{
		//		for (var i = 0; i < _connections; i++)
		//		{
		//			using (var httpClient = new HttpClient())
		//			{
		//				var result = httpClient.GetAsync(new Uri("http://bing.com")).Result;
		//			}
		//		}
		//	}
		//	catch (Exception exception)
		//	{
		//		Console.WriteLine(exception);
		//	}
		//}

		//private static void TestHttpClientWithStaticInstance()
		//{
		//	try
		//	{
		//		for (var i = 0; i < _connections; i++)
		//		{
		//			var result = _httpClient.GetAsync(new Uri("http://bing.com")).Result;
		//		}
		//	}
		//	catch (Exception exception)
		//	{
		//		Console.WriteLine(exception);
		//	}
		//}

		static Task InvokeAsync(Func<string> action)
		{
			action();
			return Task.CompletedTask;
		}

		static Task DoNothing() => Task.CompletedTask;


		internal class Test
		{
			public static void Method(Program.InvokeMethod instance, object[] arg)
			{
				int numArray = 1;
				int[] args = new int[arg.Length - numArray];
				Array.Copy(arg, numArray, args, 0, args.Length);
				instance.VoidMethodWithParams8((string)((string)arg[0]), args);
			}
		}

		[STAThread]
		async static Task Main()
		{
			//var obj = Ecng.Common.Do.Invariant(() =>
			//{
			//	ISerializer serializer = new JsonSerializer<SettingsStorage>
			//	{
			//		Indent = true,
			//		EnumAsString = true,
			//		NullValueHandling = NullValueHandling.Ignore,
			//	};
			//	return serializer.Deserialize(File.ReadAllBytes(@"C:\StockSharp\sma_enc.json"));
			//});
			var flags = BindingFlags.Public | BindingFlags.Instance;

			if (false)
				flags |= BindingFlags.IgnoreCase;
			Console.WriteLine(typeof(SberRestRegisterResponse).GetProperty(nameof(SberRestRegisterResponse.ErrorCode).ToLowerInvariant(), flags));
			return;
			//AssemblyHolder.Settings = new()
			//{
			//	AssemblyCachePath = "asms"
			//};
			//new InvokeMethod().SetValue(nameof(InvokeMethod.VoidMethodWithParams8), new object[] { "Mark Twain", 1, 10 });
			//return;
			//Console.WriteLine(typeof(int[]).GetItemType());
			//return;

			//var sync = new AsyncReaderWriterLock();
			//var dict = new Dictionary<int, TaskCompletionSource<string>>();

			////(await dict.SafeAddAsync(sync, 1, (k, t) => k.ToString().FromResult(), default)).AssertEqual("1");
			//Console.WriteLine(await dict.SafeAddAsync(sync, 2, (k, t) => k.ToString().FromResult(), default));
			////(await dict.SafeAddAsync(sync, 3, (k, t) => k.ToString().FromResult(), default)).AssertEqual("3");
			//Console.WriteLine(await dict.SafeAddAsync(sync, 2, (k, t) => k.ToString().FromResult(), default));

			////byte[] _initVectorBytes = "ss14fgty650h8u82".ASCII();
			////var txt = File.ReadAllText(@"C:\StockSharp\1.txt").Base64().Decrypt("qwerty", _initVectorBytes, _initVectorBytes).UTF8();
			////Console.WriteLine(txt.Length);
			////await InvokeAsync(async () =>
			////{
			////	Console.WriteLine("123");
			////	await DoNothing();
			////	return "123";
			////});
			//return;

			using var http = new HttpClient();

			var sber = new SberRestClient(http) { Tracing = true };

			//var rubIso4217 = CurrencyCodesResolver.GetCurrenciesByCode(CurrencyTypes.RUB.To<string>()).First().Num;
			var res3 = await sber.RegisterAsync("hsgndkuf00459gfm0k6e49om8m", Guid.NewGuid().ToString().Remove("-"), "39000", "643", "https://stocksharp.com/pay/status/?data=AQAAAAAAAACPpDnxXQs4AIJUDggrVbWI", "https://stocksharp.com/pay/status/?data=AQAAAAAAAACPpDnxXQs4AIJUDggrVbWI", "shuobi-(n29911-2021-10-08_19_24)", "en", string.Empty, string.Empty);

			Console.WriteLine(res3.ErrorMessage);

			if (!res3.FormUrl.IsEmpty())
				res3.FormUrl.OpenLink(false);

			return;

			//await Do(new TestJsonObj
			//{
			//	IntProp = 123,
			//	DateProp = DateTime.UtcNow,
			//	TimeProp = TimeSpan.FromSeconds(10),
			//	Obj1 = new TestJson2Obj
			//	{
			//		IntProp = 456,
			//		DateProp = DateTime.UtcNow.AddDays(-10),
			//		TimeProp = TimeSpan.FromSeconds(-10),
			//	},
			//	Obj2 = new[]
			//	{
			//		null,
			//		new TestJson2Obj
			//		{
			//			IntProp = 124,
			//			DateProp = DateTime.UtcNow,
			//			TimeProp = TimeSpan.FromSeconds(10),
			//		},
			//		null,
			//		new TestJson2Obj
			//		{
			//			IntProp = 124,
			//			DateProp = DateTime.UtcNow,
			//			TimeProp = TimeSpan.FromSeconds(10),
			//		},
			//	}
			//});

			//await Do(new SettingsStorage()
			//	.Set("IntProp", 124)
			//	.Set("DateProp", DateTime.UtcNow)
			//	.Set("TimeProp", TimeSpan.FromSeconds(10))
			//	.Set("ArrProp", new[] { 1, 2, 3 })
			//	.Set("ArrProp2", new[] { "123", null, string.Empty, null })
			//	.Set("NullProp", (string)null)
			//	.Set("ComplexProp1", new SettingsStorage())
			//	.Set("ComplexProp2", new SettingsStorage()
			//		.Set("IntProp", 124)
			//		.Set("DateProp", DateTime.UtcNow)
			//		.Set("TimeProp", TimeSpan.FromSeconds(10))
			//		.Set("ComplexProp1", new SettingsStorage())
			//		.Set("IntProp2", 124)
			//		.Set("ComplexProp2", new SettingsStorage()
			//			.Set("IntProp", 124)
			//			.Set("DateProp", DateTime.UtcNow)
			//			.Set("TimeProp", TimeSpan.FromSeconds(10))
			//			)
			//		)
			//	.Set("ArrComplexProp2", new[]
			//	{
			//		new SettingsStorage()
			//			.Set("IntProp", 124)
			//			.Set("DateProp", DateTime.UtcNow)
			//			.Set("TimeProp", TimeSpan.FromSeconds(10)),
			//		null,
			//		new SettingsStorage(),
			//		new SettingsStorage()
			//			.Set("IntProp", 345)
			//			.Set("DateProp", DateTime.UtcNow)
			//			.Set("TimeProp", TimeSpan.FromSeconds(10)),
			//	})
			//);

			//ISerializer ser = new JsonSerializer<TestJsonObj[]>();
			//Console.WriteLine(ser.Serialize(new TestJsonObj[] { null, new TestJsonObj { Obj1 = new TestJson2Obj() } }).UTF8());
			//Console.WriteLine(ser.Deserialize(ser.Serialize(new TestJsonObj[] { null, new TestJsonObj { Obj1 = new TestJson2Obj() } })));
			//return;

			//using (var db = new DataConnection(new SQLiteDataProvider("SQLite"), @"Data Source=..\..\TestData2.sqlite"))
			//using var db = new DataConnection(new SqlServerDataProvider("SqlSrv", SqlServerVersion.v2017), @"Data Source=test");
			//db.Connection.Open();
			//db.MappingSchema.GetFluentMappingBuilder()
			//	.Entity<TestTable>()
			//		.Property(t => t.Id)
			//			//.IsIdentity()
			//			.IsPrimaryKey()
			//		.Property(t => t.Field1)
			//			.HasLength(50);

			//db.DropTable<TestTable>(throwExceptionIfNotExists: false);

			//var table = db.CreateTable<TestTable>();
			//var table = db.GetTable<TestTable>();
			//var br = table.BulkCopy(new[] { new TestTable { Id = 1, Field1 = "123" }, new TestTable { Id = 2, Field1 = "456" } });
			//Console.WriteLine(br.Abort);
			//var list = table.ToList();

			//Console.WriteLine(list.Count);

			//db.DropTable<TestTable>();

			return;

			using var repository = new Repository(@"C:\StockSharp\Public\ecng");
			EnsureClean(repository);
			SyncRepo(repository, null, null);
			return;

			//var database = new LogicDatabase("StockSharp Database", "Server=db.stocksharp.com;Database=StockSharp;User ID=StockSharp;Password=;") { CommandType = CommandType.StoredProcedure };
			//ConfigManager.RegisterService<IStorage>(database);

			//var rootObject = new SiteRootObject("Site Root", database);
			//rootObject.Initialize();

			//var email = rootObject.Emails.ReadById(1844426L);
			//{
			//	foreach (var file in email.Files)
			//	{

			//	}
			//}

			IBackupService backSvc = new AmazonS3Service("eu-central-1", "stocksharpfiles", "", "");
			var entity = new BackupEntry { Name = "108297.bin" };
			//var s = new MemoryStream();
			//backSvc.DownloadAsync(entity, s, null, null, Console.WriteLine).Wait();
			Console.WriteLine(backSvc.PublishAsync(entity).Result);
			backSvc.UnPublishAsync(entity).Wait();
			return;

			var res = new RoslynCompilerService().Compile("LOG([USD##CASH@IDEALPRO]) - (USD_CASH@IDEALPRO / usd_cash@IDEALPRO) + LOG([USD##CASH@IDEALPRO])", true);
			Console.WriteLine(res.Calculate(new[] { 10m, 11m }));
			return;

			var settingSotrage = new SettingsStorage();
			settingSotrage.SetValue("1", TimeZoneInfo.Local);

			//settingSotrage = new XmlSerializer<SettingsStorage>().Deserialize(new XmlSerializer<SettingsStorage>().Serialize(settingSotrage));
			Console.WriteLine(settingSotrage["1"]);
			return;

			//Console.WriteLine(new Version(4, 3, 14, 0).Compare(new Version(4, 3, 14, 1)));

			/*
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
			//		if (entry.Name.EqualsIgnoreCase("stocksharp"))
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

			*/
		}
	}
}