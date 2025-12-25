namespace Ecng.Tests.Common;

using System.Globalization;
using System.Threading;

using Ecng.Common;

[TestClass]
public class DoTests : BaseTestClass
{
	#region WithCulture tests

	[TestMethod]
	public void WithCulture_SetsCulture()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		var testCulture = new CultureInfo("fr-FR");

		using (Do.WithCulture(testCulture))
		{
			Thread.CurrentThread.CurrentCulture.AssertEqual(testCulture);
		}

		Thread.CurrentThread.CurrentCulture.AssertEqual(originalCulture);
	}

	[TestMethod]
	public void WithCulture_RestoresCultureOnDispose()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		var testCulture = new CultureInfo("de-DE");

		var holder = Do.WithCulture(testCulture);
		Thread.CurrentThread.CurrentCulture.AssertEqual(testCulture);

		holder.Dispose();
		Thread.CurrentThread.CurrentCulture.AssertEqual(originalCulture);
	}

	[TestMethod]
	public void WithCulture_NestedCultures_RestoresCorrectly()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		var culture1 = new CultureInfo("fr-FR");
		var culture2 = new CultureInfo("de-DE");

		using (Do.WithCulture(culture1))
		{
			Thread.CurrentThread.CurrentCulture.AssertEqual(culture1);

			using (Do.WithCulture(culture2))
			{
				Thread.CurrentThread.CurrentCulture.AssertEqual(culture2);
			}

			Thread.CurrentThread.CurrentCulture.AssertEqual(culture1);
		}

		Thread.CurrentThread.CurrentCulture.AssertEqual(originalCulture);
	}

	#endregion

	#region WithInvariantCulture tests

	[TestMethod]
	public void WithInvariantCulture_SetsInvariantCulture()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;

		using (Do.WithInvariantCulture())
		{
			Thread.CurrentThread.CurrentCulture.AssertEqual(CultureInfo.InvariantCulture);
		}

		Thread.CurrentThread.CurrentCulture.AssertEqual(originalCulture);
	}

	#endregion

	#region Invariant tests

	[TestMethod]
	public void Invariant_Func_ExecutesUnderInvariantCulture()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

		try
		{
			var result = Do.Invariant(() =>
			{
				Thread.CurrentThread.CurrentCulture.AssertEqual(CultureInfo.InvariantCulture);
				return 42;
			});

			result.AssertEqual(42);
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public void Invariant_Func_RestoresCulture()
	{
		var testCulture = new CultureInfo("ja-JP");
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = testCulture;

		try
		{
			Do.Invariant(() => "test");
			Thread.CurrentThread.CurrentCulture.AssertEqual(testCulture);
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public void Invariant_Action_ExecutesUnderInvariantCulture()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");

		try
		{
			var executed = false;
			Do.Invariant(() =>
			{
				Thread.CurrentThread.CurrentCulture.AssertEqual(CultureInfo.InvariantCulture);
				executed = true;
			});

			executed.AssertTrue();
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public void Invariant_Action_RestoresCulture()
	{
		var testCulture = new CultureInfo("ko-KR");
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = testCulture;

		try
		{
			Do.Invariant(() => { });
			Thread.CurrentThread.CurrentCulture.AssertEqual(testCulture);
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public void Invariant_FormatsNumbersCorrectly()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE"); // Uses comma as decimal separator

		try
		{
			var result = Do.Invariant(() => 1234.56.ToString("F2"));
			result.AssertEqual("1234.56"); // Invariant uses dot
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	#endregion

	#region InvariantAsync tests

	[TestMethod]
	public async Task InvariantAsync_Func_ExecutesUnderInvariantCulture()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("es-ES");

		try
		{
			var result = await Do.InvariantAsync(async () =>
			{
				Thread.CurrentThread.CurrentCulture.AssertEqual(CultureInfo.InvariantCulture);
				await Task.Delay(1);
				return 123;
			});

			result.AssertEqual(123);
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public async Task InvariantAsync_Func_NullFunc_Throws()
	{
		await ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await Do.InvariantAsync<int>(null));
	}

	[TestMethod]
	public async Task InvariantAsync_Action_ExecutesUnderInvariantCulture()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

		try
		{
			var executed = false;
			await Do.InvariantAsync(async () =>
			{
				Thread.CurrentThread.CurrentCulture.AssertEqual(CultureInfo.InvariantCulture);
				await Task.Delay(1);
				executed = true;
			});

			executed.AssertTrue();
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public async Task InvariantAsync_Action_NullAction_Throws()
	{
		await ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await Do.InvariantAsync(null));
	}

	#endregion

	#region TryGetUniqueMutex tests

	[TestMethod]
	public void TryGetUniqueMutex_AcquiresMutex()
	{
		var mutexName = $"TestMutex_{Guid.NewGuid()}";

		var result = Do.TryGetUniqueMutex(mutexName, out var mutex);

		try
		{
			result.AssertTrue();
			mutex.AssertNotNull();
		}
		finally
		{
			mutex?.ReleaseMutex();
			mutex?.Dispose();
		}
	}

	[TestMethod]
	public void TryGetUniqueMutex_SecondAcquisition_FromDifferentThread_Fails()
	{
		var mutexName = $"TestMutex_{Guid.NewGuid()}";

		var result1 = Do.TryGetUniqueMutex(mutexName, out var mutex1);

		try
		{
			result1.AssertTrue();

			// Mutex is recursive for same thread, so we need to test from a different thread
			bool? result2 = null;
			Mutex mutex2 = null;

			var thread = new Thread(() =>
			{
				result2 = Do.TryGetUniqueMutex(mutexName, out mutex2);
			});
			thread.Start();
			thread.Join();

			try
			{
				result2.AssertNotNull();
				result2.Value.AssertFalse();
			}
			finally
			{
				mutex2?.Dispose();
			}
		}
		finally
		{
			mutex1?.ReleaseMutex();
			mutex1?.Dispose();
		}
	}

	[TestMethod]
	public void TryGetUniqueMutex_AfterRelease_CanReacquire()
	{
		var mutexName = $"TestMutex_{Guid.NewGuid()}";

		var result1 = Do.TryGetUniqueMutex(mutexName, out var mutex1);
		result1.AssertTrue();

		mutex1.ReleaseMutex();
		mutex1.Dispose();

		var result2 = Do.TryGetUniqueMutex(mutexName, out var mutex2);

		try
		{
			result2.AssertTrue();
		}
		finally
		{
			mutex2?.ReleaseMutex();
			mutex2?.Dispose();
		}
	}

	[TestMethod]
	public void TryGetUniqueMutex_DifferentNames_BothSucceed()
	{
		var mutexName1 = $"TestMutex_{Guid.NewGuid()}";
		var mutexName2 = $"TestMutex_{Guid.NewGuid()}";

		var result1 = Do.TryGetUniqueMutex(mutexName1, out var mutex1);
		var result2 = Do.TryGetUniqueMutex(mutexName2, out var mutex2);

		try
		{
			result1.AssertTrue();
			result2.AssertTrue();
		}
		finally
		{
			mutex1?.ReleaseMutex();
			mutex1?.Dispose();
			mutex2?.ReleaseMutex();
			mutex2?.Dispose();
		}
	}

	#endregion
}
