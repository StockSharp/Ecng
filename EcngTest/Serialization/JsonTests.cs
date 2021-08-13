namespace Ecng.Test.Serialization
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.UnitTesting;
	using Ecng.Reflection;

	[TestClass]
	public class JsonTests
	{
		private static async Task Do<T>(T value, CancellationToken token = default)
		{
			var ser = new JsonSerializer<T>();
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

		[TestMethod]
		public async Task Primitive()
		{
			await Do(123);
			await Do("123");
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
		}

		[TestMethod]
		public async Task PrimitiveArray()
		{
			await Do(new[] { 1, 2, 3 });
			await Do(new[] { "123", "456" });
			await Do(new[] { null, "123", "456" });
			await Do(new[] { null, "", "123", "", "456" });
			await Do(new[] { "", null, "123", null, "456", null });
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
		}

		[TestMethod]
		public async Task PrimitiveArrayOfArray()
		{
			await Do(new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } });
			await Do(new[] { new[] { "123", "456" }, new[] { "789", "000" } });
			await Do(new[] { null, new[] { null, "123", "456" }, new[] { "789", "000", null } });
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
				.Set("IntProp", 124)
				.Set("DateProp", DateTime.UtcNow)
				.Set("TimeProp", TimeSpan.FromSeconds(10))
				.Set("ArrProp", new[] { 1, 2, 3 })
				.Set("ArrProp2", new[] { "123", null, string.Empty, null })
				.Set("ComplexProp1", new SettingsStorage())
				.Set("ComplexProp2", new SettingsStorage()
					.Set("IntProp", 124)
					.Set("DateProp", DateTime.UtcNow)
					.Set("TimeProp", TimeSpan.FromSeconds(10)))
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
			public TimeSpan TimeProp { get; set; }

			public override TestClass Clone()
			{
				return PersistableHelper.Clone(this);
			}

			public override bool Equals(TestClass other)
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

			public override bool Equals(TestClassAsync other)
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
			public TestClass Obj1 { get; set; }
			public TestClass[] Obj2 { get; set; }

			public override TestComplexClass Clone()
			{
				return PersistableHelper.Clone(this);
			}

			public override bool Equals(TestComplexClass other)
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
	}
}