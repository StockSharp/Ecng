namespace Ecng.Tests.Common;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

[TestClass]
public class DelegateTests : BaseTestClass
{
	[TestMethod]
	public void DoAsync_ExecutesActionAsynchronously()
	{
		// Arrange
		var executed = false;
		Exception caughtError = null;
		var waitHandle = new ManualResetEvent(false);

		Action action = () =>
		{
			executed = true;
			waitHandle.Set();
		};

		Action<Exception> errorHandler = ex =>
		{
			caughtError = ex;
			waitHandle.Set();
		};

		// Act
		action.DoAsync(errorHandler);
		var signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));

		// Assert
		signaled.AssertTrue();
		executed.AssertTrue();
		caughtError.AssertNull();
	}

	[TestMethod]
	public async Task DoAsync_ModernReplacement_WithTaskRun()
	{
		// Arrange
		var executed = false;
		Exception caughtError = null;

		Action action = () =>
		{
			executed = true;
		};

		// Act - Modern replacement using Task.Run
		await Task.Run(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				caughtError = ex;
			}
		});

		// Assert
		executed.AssertTrue();
		caughtError.AssertNull();
	}
}
