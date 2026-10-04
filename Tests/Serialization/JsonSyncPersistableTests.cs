#pragma warning disable CS0618 // the obsolete contract is what these tests are about
namespace Ecng.Tests.Serialization;

using System.Drawing;

using Ecng.IO;
using Ecng.Serialization;
using Ecng.Reflection;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

/// <summary>
/// Round-trips of types that persist themselves through the obsolete <see cref="IPersistable"/>.
/// </summary>
[TestClass]
public class JsonSyncPersistableTests : BaseTestClass
{
	private async Task<T> Do<T>(T value, bool fillMode = false, bool enumAsString = false, bool encryptedAsByteArray = false, NullValueHandling nullValueHandling = NullValueHandling.Include, Action<string> jsonInspector = null)
	{
		var ser = new JsonSerializer<T>
		{
			FillMode = fillMode,
			EnumAsString = enumAsString,
			EncryptedAsByteArray = encryptedAsByteArray,
			NullValueHandling = nullValueHandling
		};

		var stream = new MemoryStream();
		await ser.SerializeAsync(value, stream, CancellationToken);

		// Verify intermediate format - stream should have content
		stream.Length.AssertGreater(0L, "Serialized JSON stream should not be empty");

		// Capture JSON for inspection if needed
		stream.Position = 0;
		var jsonBytes = stream.ToArray();
		var jsonString = jsonBytes.UTF8();

		// Allow tests to inspect the intermediate JSON format
		jsonInspector?.Invoke(jsonString);

		stream.Position = 0;

		var needCast = value is SettingsStorage;

		var actual = await ser.DeserializeAsync(stream, CancellationToken);

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

			if (expected is SettingsStorage expStorage)
			{
				actual.AssertNotNull();

				var actStorage = (SettingsStorage)actual;
				actStorage.Count.AssertEqual(expStorage.Count);

				foreach (var key in expStorage.Keys)
				{
					var expItem = expStorage[key];
					var actItem = actStorage.GetValue(expItem is Type ? typeof(Type) : expItem.GetType(), key);

					if (expItem is IEnumerable)
						CheckValue(actItem, expItem);
					else
						AssertEqual(actItem, expItem);
				}
			}
			else if (expected is IEnumerable expEmu)
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

		return actual;
	}

	private class TestClass : Equatable<TestClass>, IPersistable
	{
		public int IntProp { get; set; }
		public DateTime DateProp { get; set; }
		public string StringProp { get; set; }
		public SecureString SecureStringProp { get; set; }
		public TimeSpan TimeProp { get; set; }
		public Type TypeProp { get; set; }

		public override TestClass Clone()
		{
			return (TestClass)MemberwiseClone();
		}

		protected override bool OnEquals(TestClass other)
		{
			return
				IntProp == other.IntProp &&
				DateProp == other.DateProp &&
				StringProp == other.StringProp &&
				SecureStringProp.IsEqualTo(other.SecureStringProp) &&
				TimeProp == other.TimeProp &&
				TypeProp == other.TypeProp
				;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			IntProp = storage.GetValue<int>(nameof(IntProp));
			DateProp = storage.GetValue<DateTime>(nameof(DateProp));
			StringProp = storage.GetValue<string>(nameof(StringProp));
			SecureStringProp = storage.GetValue<SecureString>(nameof(SecureStringProp));
			TimeProp = storage.GetValue<TimeSpan>(nameof(TimeProp));
			TypeProp = storage.GetValue<Type>(nameof(TypeProp));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(IntProp), IntProp)
				.Set(nameof(DateProp), DateProp)
				.Set(nameof(StringProp), StringProp)
				.Set(nameof(SecureStringProp), SecureStringProp)
				.Set(nameof(TimeProp), TimeProp)
				.Set(nameof(TypeProp), TypeProp);
		}
	}

	[TestMethod]
	public async Task Complex()
	{
		await Do(new TestClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
		});
	}

	[TestMethod]
	public async Task Complex2()
	{
		await Do(new TestClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			StringProp = "123",
		});
	}

	[TestMethod]
	public async Task Complex3()
	{
		await Do(new TestClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			SecureStringProp = "123".Secure(),
		});
	}

	[TestMethod]
	public async Task Complex4()
	{
		await Do(new TestClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			StringProp = "123",
			TypeProp = typeof(GCKind),
		});
	}

	[TestMethod]
	public async Task ComplexArray()
	{
		await Do(new[]
		{
			new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			},
			new TestClass
			{
				IntProp = 456,
				DateProp = DateTime.UtcNow.AddDays(1),
				TimeProp = TimeSpan.FromSeconds(20),
			},
		});
	}

	[TestMethod]
	public async Task ComplexEnumerable()
	{
		await Do<IEnumerable<TestClass>>(
		[
			new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			},
			new TestClass
			{
				IntProp = 456,
				DateProp = DateTime.UtcNow.AddDays(1),
				TimeProp = TimeSpan.FromSeconds(20),
			},
		]);
	}

	[TestMethod]
	public async Task ComplexArrayOfArray()
	{
		await Do(new[]
		{
			new[]
			{
				new TestClass
				{
					IntProp = 124,
					DateProp = DateTime.UtcNow,
					TimeProp = TimeSpan.FromSeconds(10),
				},
				null,
				new TestClass
				{
					IntProp = 456,
					DateProp = DateTime.UtcNow.AddDays(1),
					TimeProp = TimeSpan.FromSeconds(20),
				},
			},
			null,
			[
				new TestClass
				{
					IntProp = 124,
					DateProp = DateTime.UtcNow,
					TimeProp = TimeSpan.FromSeconds(10),
				},
				null,
				new TestClass
				{
					IntProp = 456,
					DateProp = DateTime.UtcNow.AddDays(1),
					TimeProp = TimeSpan.FromSeconds(20),
				},
			],
		});
	}

	[TestMethod]
	public async Task ComplexArrayWithNull()
	{
		await Do(new[]
		{
			null,
			new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			},
			new TestClass
			{
				IntProp = 456,
				DateProp = DateTime.UtcNow.AddDays(1),
				TimeProp = TimeSpan.FromSeconds(20),
			},
		});

		await Do(new[]
		{
			null,
			new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			},
			new TestClass
			{
				IntProp = 456,
				DateProp = DateTime.UtcNow.AddDays(1),
				TimeProp = TimeSpan.FromSeconds(20),
			},
			null,
		});
	}

	[TestMethod]
	public async Task EntireAsync_RoundTripsSynchronouslyPersistedObject()
	{
		var source = new TestClass
		{
			IntProp = 11,
			DateProp = DateTime.UtcNow,
			StringProp = "entire",
			TimeProp = TimeSpan.FromSeconds(5),
		};

		var storage = await source.SaveEntireAsync(false, CancellationToken);
		var restored = await storage.LoadEntireAsync<IPersistable>(CancellationToken);

		IsInstanceOfType<TestClass>(restored);
		AreEqual(source.IntProp, ((TestClass)restored).IntProp);
		AreEqual(source.StringProp, ((TestClass)restored).StringProp);
	}

	[TestMethod]
	public async Task CloneAsync_ClonesSynchronouslyPersistedObject()
	{
		var source = new TestClass { IntProp = 3, StringProp = "clone" };

		var clone = await source.CloneAsync(CancellationToken);

		AreNotSame(source, clone);
		AreEqual(source.IntProp, clone.IntProp);
		AreEqual(source.StringProp, clone.StringProp);
	}

	[TestMethod]
	public async Task AsynchronousForms_PreferAsynchronousPersistence()
	{
		var source = new DualPersistable { Value = 5 };

		var clone = await source.CloneAsync(CancellationToken);

		AreEqual(5, clone.Value);
		IsTrue(clone.IsLoadedAsynchronously);
	}

	private class DualPersistable : IPersistable, IAsyncPersistable
	{
		public int Value { get; set; }
		public bool IsLoadedAsynchronously { get; private set; }

		void IPersistable.Load(SettingsStorage storage) => Value = storage.GetValue<int>(nameof(Value));

		void IPersistable.Save(SettingsStorage storage) => storage.Set(nameof(Value), Value);

		Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			Value = storage.GetValue<int>(nameof(Value));
			IsLoadedAsynchronously = true;
			return Task.CompletedTask;
		}

		Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			storage.Set(nameof(Value), Value);
			return Task.CompletedTask;
		}
	}

	private class TestComplexClass : Equatable<TestComplexClass>, IPersistable
	{
		public int IntProp { get; set; }
		public DateTime DateProp { get; set; }
		public TimeSpan TimeProp { get; set; }
		public TimeSpan[] TimeArrayProp { get; set; }
		public TestClass Obj1 { get; set; }
		public TimeSpan[] TimeArray2Prop { get; set; }
		public TestClass[] Obj2 { get; set; }
		public string[] StringArrayProp { get; set; }
		public string[] StringArray2Prop { get; set; }
		public SecureString[] SecureStringArrayProp { get; set; }

		public override TestComplexClass Clone()
		{
			return (TestComplexClass)MemberwiseClone();
		}

		protected override bool OnEquals(TestComplexClass other)
		{
			return
				IntProp == other.IntProp &&
				DateProp == other.DateProp &&
				TimeProp == other.TimeProp &&
				Obj1 == other.Obj1 &&
				((Obj2 is null && other.Obj2 is null) || Obj2?.SequenceEqual(other.Obj2) == true) &&
				((TimeArrayProp is null && other.TimeArrayProp is null) || TimeArrayProp?.SequenceEqual(other.TimeArrayProp) == true) &&
				((TimeArray2Prop is null && other.TimeArray2Prop is null) || TimeArray2Prop?.SequenceEqual(other.TimeArray2Prop) == true) &&
				((StringArrayProp is null && other.StringArrayProp is null) || StringArrayProp?.SequenceEqual(other.StringArrayProp) == true) &&
				((StringArray2Prop is null && other.StringArray2Prop is null) || StringArray2Prop?.SequenceEqual(other.StringArray2Prop) == true) &&
				((SecureStringArrayProp is null && other.SecureStringArrayProp is null) || SecureStringArrayProp?.SequenceEqual(other.SecureStringArrayProp, StringHelper.IsEqualTo) == true)
				;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			IntProp = storage.GetValue<int>(nameof(IntProp));
			DateProp = storage.GetValue<DateTime>(nameof(DateProp));
			TimeProp = storage.GetValue<TimeSpan>(nameof(TimeProp));

			TimeArrayProp = storage.GetValue<TimeSpan[]>(nameof(TimeArrayProp));
			TimeArray2Prop = storage.GetValue<TimeSpan[]>(nameof(TimeArray2Prop));
			StringArrayProp = storage.GetValue<string[]>(nameof(StringArrayProp));
			StringArray2Prop = storage.GetValue<string[]>(nameof(StringArray2Prop));
			SecureStringArrayProp = storage.GetValue<SecureString[]>(nameof(SecureStringArrayProp));

			//if (storage.ContainsKey(nameof(Obj1)))
			Obj1 = storage.GetValue<SettingsStorage>(nameof(Obj1))?.Load<TestClass>();

			//if (storage.ContainsKey(nameof(Obj2)))
			Obj2 = storage.GetValue<SettingsStorage[]>(nameof(Obj2))?.Select(s => s?.Load<TestClass>()).ToArray();
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(IntProp), IntProp)
				.Set(nameof(DateProp), DateProp)
				.Set(nameof(TimeProp), TimeProp)
				.Set(nameof(TimeArrayProp), TimeArrayProp)
				.Set(nameof(TimeArray2Prop), TimeArray2Prop)
				.Set(nameof(StringArrayProp), StringArrayProp)
				.Set(nameof(StringArray2Prop), StringArray2Prop)
				.Set(nameof(SecureStringArrayProp), SecureStringArrayProp)
				.Set(nameof(Obj1), Obj1?.Save())
				.Set(nameof(Obj2), Obj2?.Select(o => o?.Save()));
		}
	}

	[TestMethod]
	public async Task ComplexComplex()
	{
		await Do(new TestComplexClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
		});
	}

	[TestMethod]
	public async Task ComplexComplexIgnoreNull()
	{
		await Do(new TestComplexClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
		}, fillMode: true, nullValueHandling: NullValueHandling.Ignore);
	}

	[TestMethod]
	public async Task ComplexComplexNull()
	{
		await Do<TestComplexClass>(null);
	}

	[TestMethod]
	public async Task ComplexComplex2()
	{
		await Do(new TestComplexClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj1 = new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			}
		});
	}

	[TestMethod]
	public async Task ComplexComplex2IgnoreNull()
	{
		await Do(new TestComplexClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj1 = new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			}
		}, fillMode: true, nullValueHandling: NullValueHandling.Ignore);
	}

	[TestMethod]
	public async Task ComplexComplex3()
	{
		await Do(new TestComplexClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj1 = new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			},
			TimeArrayProp = [TimeSpan.FromSeconds(10)],
			StringArray2Prop = [null, "", "123"],
			SecureStringArrayProp = [null, "".Secure(), "123".Secure()],
			Obj2 =
			[
				null,
				new TestClass
				{
					IntProp = 124,
					DateProp = DateTime.UtcNow,
					TimeProp = TimeSpan.FromSeconds(10),
				},
				null,
				new TestClass
				{
					IntProp = 124,
					DateProp = DateTime.UtcNow,
					TimeProp = TimeSpan.FromSeconds(10),
				},
			]
		});
	}

	private class TestContainsClass : Equatable<TestContainsClass>, IPersistable
	{
		public int IntProp { get; set; }
		public DateTime DateProp { get; set; }
		public TimeSpan TimeProp { get; set; }
		public TestClass Obj1 { get; set; }
		public TestClass[] Obj2 { get; set; }

		public override TestContainsClass Clone()
		{
			return (TestContainsClass)MemberwiseClone();
		}

		protected override bool OnEquals(TestContainsClass other)
		{
			return
				IntProp == other.IntProp &&
				DateProp == other.DateProp &&
				TimeProp == other.TimeProp &&
				Obj1 == other.Obj1 &&
				((Obj2 is null && other.Obj2 is null) || Obj2?.SequenceEqual(other.Obj2) == true)
				;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			IntProp = storage.GetValue<int>(nameof(IntProp));
			DateProp = storage.GetValue<DateTime>(nameof(DateProp));
			TimeProp = storage.GetValue<TimeSpan>(nameof(TimeProp));

			if (storage.ContainsKey(nameof(Obj1)))
				Obj1 = storage.GetValue<SettingsStorage>(nameof(Obj1)).Load<TestClass>();

			if (storage.ContainsKey(nameof(Obj2)))
				Obj2 = [.. storage.GetValue<object[]>(nameof(Obj2)).Select(s => ((SettingsStorage)s)?.Load<TestClass>())];
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(IntProp), IntProp)
				.Set(nameof(DateProp), DateProp)
				.Set(nameof(TimeProp), TimeProp);

			if (Obj1 != null)
				storage.Set(nameof(Obj1), Obj1.Save());

			if (Obj2 != null)
				storage.Set(nameof(Obj2), Obj2.Select(o => o?.Save()));
		}
	}

	[TestMethod]
	public async Task Contains()
	{
		await Do(new TestContainsClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj1 = new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			}
		}, fillMode: true);

		await Do(new TestContainsClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj2 = [],
		}, fillMode: true);

		await Do(new TestContainsClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj2 = [null],
		}, fillMode: true);
	}

	[TestMethod]
	public async Task ContainsNull()
	{
		await Do<TestContainsClass>(null, true);
	}

	private class TestDirectClass : Equatable<TestDirectClass>, IPersistable
	{
		public int IntProp { get; set; }
		public DateTime DateProp { get; set; }
		public TimeSpan TimeProp { get; set; }
		public TestClass Obj1 { get; set; }
		public TestClass[] Obj2 { get; set; }

		public override TestDirectClass Clone()
		{
			return (TestDirectClass)MemberwiseClone();
		}

		protected override bool OnEquals(TestDirectClass other)
		{
			return
				IntProp == other.IntProp &&
				DateProp == other.DateProp &&
				TimeProp == other.TimeProp &&
				Obj1 == other.Obj1 &&
				((Obj2 is null && other.Obj2 is null) || Obj2?.SequenceEqual(other.Obj2) == true)
				;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			IntProp = storage.GetValue<int>(nameof(IntProp));
			DateProp = storage.GetValue<DateTime>(nameof(DateProp));
			TimeProp = storage.GetValue<TimeSpan>(nameof(TimeProp));

			Obj1 = storage.GetValue<TestClass>(nameof(Obj1));

			if (storage.ContainsKey(nameof(Obj2)))
				Obj2 = storage.GetValue<TestClass[]>(nameof(Obj2));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(IntProp), IntProp)
				.Set(nameof(DateProp), DateProp)
				.Set(nameof(TimeProp), TimeProp);

			if (Obj1 != null)
				storage.Set(nameof(Obj1), Obj1.Save());

			if (Obj2 != null)
				storage.Set(nameof(Obj2), Obj2.Select(o => o?.Save()));
		}
	}

	[TestMethod]
	public async Task Direct()
	{
		await Do(new TestDirectClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj1 = new TestClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			}
		}, fillMode: true);

		await Do(new TestDirectClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj2 = [new TestClass()],
		}, fillMode: true);

		await Do(new TestDirectClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj2 = [],
		}, fillMode: true);

		await Do(new TestDirectClass
		{
			IntProp = 124,
			DateProp = DateTime.UtcNow,
			TimeProp = TimeSpan.FromSeconds(10),
			Obj2 = [null],
		}, fillMode: true);
	}

	private class TestEnumClass : Equatable<TestEnumClass>, IPersistable
	{
		public GCKind EnumProp { get; set; }
		public GCKind? NullableEnumProp { get; set; }

		public override TestEnumClass Clone()
		{
			return (TestEnumClass)MemberwiseClone();
		}

		protected override bool OnEquals(TestEnumClass other)
		{
			return
				EnumProp == other.EnumProp &&
				NullableEnumProp == other.NullableEnumProp;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			EnumProp = storage.GetValue<GCKind>(nameof(EnumProp));
			NullableEnumProp = storage.GetValue<GCKind?>(nameof(NullableEnumProp));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(EnumProp), EnumProp)
				.Set(nameof(NullableEnumProp), NullableEnumProp);
		}
	}

	[TestMethod]
	public async Task ComplexEnum()
	{
		await Do(new TestEnumClass
		{
			EnumProp = GCKind.Ephemeral,
			NullableEnumProp = GCKind.FullBlocking,
		}, enumAsString: true);

		await Do(new TestEnumClass
		{
			EnumProp = GCKind.Ephemeral,
		}, enumAsString: true);

		await Do(new TestEnumClass
		{
			EnumProp = GCKind.Ephemeral,
			NullableEnumProp = GCKind.FullBlocking,
		});

		await Do(new TestEnumClass
		{
			EnumProp = GCKind.Ephemeral,
		});
	}

	private class TestSecureString : Equatable<TestSecureString>, IPersistable
	{
		public SecureString SecureStringProp { get; set; }

		public override TestSecureString Clone()
		{
			return (TestSecureString)MemberwiseClone();
		}

		protected override bool OnEquals(TestSecureString other)
		{
			return
				SecureStringProp.IsEqualTo(other.SecureStringProp);
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			SecureStringProp = storage.GetValue<SecureString>(nameof(SecureStringProp));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(SecureStringProp), SecureStringProp);
		}
	}

	[TestMethod]
	public async Task ComplexSecureString()
	{
		const string plainTextPassword = "SuperSecret123!";
		var obj = new TestSecureString
		{
			SecureStringProp = plainTextPassword.Secure(),
		};

		// Test with encryptedAsByteArray: true - verify plain text password is NOT in JSON
		await Do(obj, encryptedAsByteArray: true, jsonInspector: json =>
		{
			json.Contains(plainTextPassword).AssertFalse($"Plain text password should NOT appear in JSON when encryptedAsByteArray=true. JSON: {json}");
		});

		// Test without encryptedAsByteArray - verify JSON has some content for the property
		await Do(obj, jsonInspector: json =>
		{
			json.Contains("SecureStringProp").AssertTrue("JSON should contain the property name");
		});

		var ss = obj.Save();

		await Do(ss, encryptedAsByteArray: true);
		await Do(ss);

		obj.AssertEqual((await Do(ss, encryptedAsByteArray: true)).Load<TestSecureString>());
		obj.AssertEqual((await Do(ss)).Load<TestSecureString>());
	}

	private struct CurrencyPersistableAdapter : IPersistableAdapter, IPersistable
	{
		private Currency _underlyingValue;

		object IPersistableAdapter.UnderlyingValue
		{
			readonly get => _underlyingValue;
			set => _underlyingValue = (Currency)value;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			_underlyingValue = new()
			{
				Type = storage.GetValue<CurrencyTypes>(nameof(_underlyingValue.Type)),
				Value = storage.GetValue<decimal>(nameof(_underlyingValue.Value))
			};
		}

		readonly void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(_underlyingValue.Type), _underlyingValue.Type)
				.Set(nameof(_underlyingValue.Value), _underlyingValue.Value);
		}
	}

	private class TestCurrencyComplex : Equatable<TestCurrencyComplex>, IPersistable
	{
		public Currency Currency { get; set; }

		protected override bool OnEquals(TestCurrencyComplex other)
		{
			return Currency == other.Currency;
		}

		public override TestCurrencyComplex Clone()
		{
			return new() { Currency = Currency };
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Currency = storage.GetValue<Currency>(nameof(Currency));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.Set(nameof(Currency), Currency);
		}
	}

	[TestMethod]
	public async Task Currency()
	{
		PersistableHelper.RegisterAdapterType(typeof(Currency), typeof(CurrencyPersistableAdapter));

		try
		{
			var curr = 10m.ToCurrency(CurrencyTypes.USD);

			await Do(curr);
			await Do<Currency>(null);
			await Do<TestCurrencyComplex>(new() { Currency = curr });
			await Do<TestCurrencyComplex>(null);
			await Do<TestCurrencyComplex>(new());
		}
		finally
		{
			PersistableHelper.RemoveAdapterType(typeof(Currency));
		}
	}

	[TestMethod]
	public void NullDeserialize()
	{
		((string)null).DeserializeObject<CurrencyTypes?>().AssertEqual(null);
		string.Empty.DeserializeObject<CurrencyTypes?>().AssertEqual(null);
		((JToken)null).DeserializeObject<CurrencyTypes?>().AssertEqual(null);

		((string)null).DeserializeObject<TestClass>().AssertEqual(null);
		string.Empty.DeserializeObject<TestClass>().AssertEqual(null);
		((JToken)null).DeserializeObject<TestClass>().AssertEqual(null);
	}

	[TestMethod]
	public async Task Bom()
	{
		var requestBody = new MemoryStream();
		await new JsonSerializer<TestClass> { Encoding = JsonHelper.UTF8NoBom }.SerializeAsync(new(), requestBody, CancellationToken);
		requestBody.To<byte[]>().UTF8().DeserializeObject<TestClass>();
	}

	[TestMethod]
	public async Task BomError()
	{
		var requestBody = new MemoryStream();
		await new JsonSerializer<TestClass>().SerializeAsync(new(), requestBody, CancellationToken);
		ThrowsExactly<InvalidOperationException>(() => requestBody.To<byte[]>().UTF8().DeserializeObject<TestClass>());
	}

	private class ExtraPropClass : IPersistable
	{
		public int IntProp { get; set; }

		void IPersistable.Load(SettingsStorage storage)
		{
			IntProp = storage.GetValue<int>(nameof(IntProp));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.Set(nameof(IntProp), IntProp);
		}
	}

	/// <summary>
	/// Regression test for FillMode=false deserialization: ensures unconsumed/extra properties left
	/// by IPersistable.Load are skipped deterministically to the matching EndObject in all build
	/// configurations, so a following element still deserializes correctly.
	/// (Was: the reader was only validated by the [Conditional("DEBUG")] CheckExpectedToken, so in
	/// Release an extra property left the reader mid-object and desynced subsequent tokens; now
	/// handled by SkipToEndObjectAsync, JsonSerializer.cs.)
	/// </summary>
	[TestMethod]
	public async Task FillModeFalse_ExtraProperty_DoesNotDesyncReader()
	{
		var ser = new JsonSerializer<ExtraPropClass[]>
		{
			FillMode = false,
		};

		// An array of two objects; the FIRST carries an extra, unconsumed property ("Extra").
		// A correct reader skips it to the object's EndObject so the SECOND object still parses.
		var json =
			"[" +
			"{\"IntProp\":11,\"Extra\":\"unconsumed\"}," +
			"{\"IntProp\":22}" +
			"]";

		using var stream = new MemoryStream(json.UTF8());

		var arr = await ser.DeserializeAsync(stream, CancellationToken);

		arr.AssertNotNull();
		arr.Length.AssertEqual(2);
		arr[0].IntProp.AssertEqual(11);
		arr[1].IntProp.AssertEqual(22);
	}

	[TestMethod]
	public async Task StringAsync_Persistable_RoundTrips()
	{
		var serializer = new JsonSerializer<SettingsStorage>();
		var original = new TestClass { IntProp = 3, StringProp = "alpha" };

		var text = await serializer.SaveToStringAsync(original, CancellationToken);

		var read = new TestClass();
		await serializer.LoadFromStringAsync(read, text, CancellationToken);

		read.AssertEqual(original);
	}
}
