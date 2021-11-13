namespace Ecng.Tests.Serialization
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq;
	using System.Security;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.UnitTesting;
	using Ecng.Reflection;
	using Newtonsoft.Json.Linq;

	[TestClass]
	public class JsonTests
	{
		private static async Task<T> Do<T>(T value, bool fillMode = false, bool enumAsString = false, bool encryptedAsByteArray = false, CancellationToken token = default)
		{
			var ser = new JsonSerializer<T> { FillMode = fillMode, EnumAsString = enumAsString, EncryptedAsByteArray = encryptedAsByteArray };
			var stream = new MemoryStream();
			await ser.SerializeAsync(value, stream, token);
			stream.Position = 0;

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

		[TestMethod]
		public async Task Primitive()
		{
			await Do(123);
			await Do("123");
			await Do("123".Secure());
			await Do(GCKind.Any);
			await Do(1324554.55M);
			await Do(DateTime.UtcNow);
			await Do(DateTimeOffset.UtcNow);
			await Do(DateTime.Now);
			await Do(DateTimeOffset.Now);
			await Do(TimeSpan.Zero);
			await Do(TimeSpan.FromSeconds(10));
			await Do(Guid.NewGuid());
			await Do(new Uri("https://stocksharp.com"));
			await Do(TimeZoneInfo.Local);
			await Do(TimeZoneInfo.Utc);
			await Do(TimeHelper.Moscow);
			await Do(typeof(GCKind));
			await Do(decimal.MinValue);
			await Do(decimal.MaxValue);
		}

		[TestMethod]
		public async Task PrimitiveNullable()
		{
			await Do((int?)null);
			await Do((string)null);
			await Do((SecureString)null);
			await Do((GCKind?)null);
			await Do((decimal?)null);
			await Do((DateTime?)null);
			await Do((DateTimeOffset?)null);
			await Do((TimeZoneInfo)null);
			await Do((TimeSpan?)null);
			await Do((Guid?)null);
			await Do((Uri)null);
			await Do((Type)null);
		}

		[TestMethod]
		public async Task PrimitiveEnumAsString()
		{
			await Do(GCKind.Any, enumAsString: true);
			await Do((GCKind?)null, enumAsString: true);
		}

		[TestMethod]
		public async Task PrimitiveArray()
		{
			await Do(new[] { 1, 2, 3 });
			await Do(new[] { "123", "456" });
			await Do(new[] { null, "123", "456" });
			await Do(new[] { null, "", "123", "", "456" });
			await Do(new[] { "", null, "123", null, "456", null });
			await Do(new[] { "".Secure(), null, "123".Secure(), null, "456".Secure(), null });
			await Do(new[] { GCKind.Any, GCKind.Background });
			await Do(new[] { 1324554.55M, 14554.55M });
			await Do(new[] { DateTime.UtcNow, DateTime.Now });
			await Do(new[] { DateTimeOffset.UtcNow, DateTimeOffset.Now });
			await Do(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(10) });
			await Do(new[] { Guid.NewGuid(), Guid.NewGuid() });
			await Do(new[] { new Uri("https://stocksharp.com"), new Uri("https://stocksharp.ru"), null });
			await Do(new[] { TimeZoneInfo.Local });
			await Do(new[] { null, TimeZoneInfo.Local });
			await Do(new byte[] { 1, 2, 3 });
			await Do(new string[] { null, null });
			await Do(new SecureString[] { null, null });
			await Do(new Type[] { null, typeof(GCKind) });
		}

		[TestMethod]
		public async Task PrimitiveArrayOfArray()
		{
			await Do(new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } });
			await Do(new[] { new[] { "123", "456" }, new[] { "789", "000" } });
			await Do(new[] { null, new[] { null, "123", "456" }, new[] { "789", "000", null } });
			await Do(new[] { null, new[] { null, "123".Secure(), "456".Secure() }, new[] { "789".Secure(), "000".Secure(), null } });
		}

		[TestMethod]
		public async Task PrimitiveArrayOfArrayOfArray()
		{
			await Do(new[] { null, new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8, 9 } }, new[] { new[] { -1, -2, -3 }, null, new[] { -4, -5, -6 }, new[] { -7, -8, -9 } } });
			await Do(new[] { new[] { new[] { "123", "456" }, null, new[] { "789", "000" } }, new[] { new[] { "123", "456" }, new[] { "123", "456" } } });
		}

		[TestMethod]
		public async Task PrimitiveLimits()
		{
			await Do(DateTime.MinValue);
			await Do(DateTime.MaxValue);
			await Do(DateTimeOffset.MinValue);
			await Do(DateTimeOffset.MaxValue);
			await Do(TimeSpan.MinValue);
			await Do(TimeSpan.MaxValue);
			await Do(decimal.MinValue);
			await Do(decimal.MaxValue);
			await Do(long.MinValue);
			await Do(long.MaxValue);
			await Do(byte.MinValue);
			await Do(byte.MaxValue);
		}

		[TestMethod]
		public async Task SettingsStorage()
		{
			var storage = new SettingsStorage()
				.Set("DecimalMax", decimal.MaxValue)
				.Set("DecimalMin", decimal.MinValue)
				.Set("IntProp", 124)
				.Set("DateProp", DateTime.UtcNow)
				.Set("TimeProp", TimeSpan.FromSeconds(10))
				.Set("ArrIntProp", new[] { 1, 2, 3 })
				.Set("ArrStringProp", new[] { "123", null, string.Empty, null })
				// TODO
				//.Set("ArrSecureStringProp", new[] { "123".Secure(), null, string.Empty.Secure(), null })
				.Set("ComplexProp1", new SettingsStorage())
				.Set("ComplexProp2", new SettingsStorage()
					.Set("IntProp", 124)
					.Set("DateProp", DateTime.UtcNow)
					.Set("TimeProp", TimeSpan.FromSeconds(10))
					.Set("TypeProp", typeof(GCKind)))
				.Set("ArrComplexProp2", new[]
				{
					new SettingsStorage()
						.Set("IntProp", 124)
						.Set("DateProp", DateTime.UtcNow)
						.Set("TimeProp", TimeSpan.FromSeconds(10)),
					null,
					new SettingsStorage(),
					new SettingsStorage()
						.Set("IntProp", 345)
						.Set("DateProp", DateTime.UtcNow)
						.Set("TimeProp", TimeSpan.FromSeconds(10)),
				});

			await Do(storage);
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
				return PersistableHelper.Clone(this);
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

		private class TestClassAsync : Equatable<TestClassAsync>, IAsyncPersistable
		{
			public int IntProp { get; set; }
			public DateTime DateProp { get; set; }
			public TimeSpan TimeProp { get; set; }

			public override TestClassAsync Clone()
			{
				return PersistableHelper.CloneAsync(this).Result;
			}

			protected override bool OnEquals(TestClassAsync other)
			{
				return
					IntProp == other.IntProp &&
					DateProp == other.DateProp &&
					TimeProp == other.TimeProp
					;
			}

			async Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
			{
				IntProp = await storage.GetValueAsync<int>(nameof(IntProp), cancellationToken: cancellationToken);
				DateProp = await storage.GetValueAsync<DateTime>(nameof(DateProp), cancellationToken: cancellationToken);
				TimeProp = await storage.GetValueAsync<TimeSpan>(nameof(TimeProp), cancellationToken: cancellationToken);
			}

			Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
			{
				storage
					.Set(nameof(IntProp), IntProp)
					.Set(nameof(DateProp), DateProp)
					.Set(nameof(TimeProp), TimeProp);

				return Task.CompletedTask;
			}
		}

		[TestMethod]
		public async Task ComplexAsync()
		{
			await Do(new TestClassAsync
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
			});
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
				return PersistableHelper.Clone(this);
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
				TimeArrayProp = new[] { TimeSpan.FromSeconds(10) },
				StringArray2Prop = new[] { null, "", "123" },
				SecureStringArrayProp = new[] { null, "".Secure(), "123".Secure() },
				Obj2 = new[]
				{
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
				}
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
				return PersistableHelper.Clone(this);
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
					Obj2 = storage.GetValue<object[]>(nameof(Obj2)).Select(s => ((SettingsStorage)s)?.Load<TestClass>()).ToArray();
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
				Obj2 = new TestClass[0],
			}, fillMode: true);

			await Do(new TestContainsClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
				Obj2 = new TestClass[] { null },
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
				return PersistableHelper.Clone(this);
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
				Obj2 = new[] { new TestClass() },
			}, fillMode: true);

			await Do(new TestDirectClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
				Obj2 = new TestClass[0],
			}, fillMode: true);

			await Do(new TestDirectClass
			{
				IntProp = 124,
				DateProp = DateTime.UtcNow,
				TimeProp = TimeSpan.FromSeconds(10),
				Obj2 = new TestClass[] { null },
			}, fillMode: true);
		}

		private class TestEnumClass : Equatable<TestEnumClass>, IPersistable
		{
			public GCKind EnumProp { get; set; }
			public GCKind? NullableEnumProp { get; set; }

			public override TestEnumClass Clone()
			{
				return PersistableHelper.Clone(this);
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
				return PersistableHelper.Clone(this);
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
			var obj = new TestSecureString
			{
				SecureStringProp = "123".Secure(),
			};

			await Do(obj, encryptedAsByteArray: true);
			await Do(obj);

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
				get => _underlyingValue;
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

			void IPersistable.Save(SettingsStorage storage)
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
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullDeserializeError1()
		{
			((string)null).DeserializeObject<CurrencyTypes>();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullDeserializeError2()
		{
			string.Empty.DeserializeObject<CurrencyTypes>();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullDeserializeError3()
		{
			((JToken)null).DeserializeObject<CurrencyTypes>();
		}
	}
}