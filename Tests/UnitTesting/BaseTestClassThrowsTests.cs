namespace Ecng.Tests.UnitTesting;

/// <summary>
/// The exception-expecting helpers of <see cref="BaseTestClass"/>.
/// </summary>
/// <remarks>
/// Every helper gets one test that drives it in both directions: it must return what it should
/// accept and it must raise <see cref="AssertFailedException"/> on what it should not. The separate
/// no-exception tests cover the failing direction alone, there being nothing there to accept. The
/// failing direction is captured with a plain try/catch rather than with the helpers themselves -
/// wrapping Throws in Throws would let a broken helper verify itself and still go green.
/// </remarks>
[TestClass]
public class BaseTestClassThrowsTests : BaseTestClass
{
	private const string _msg = "boom-42";
	private const string _custom = "custom-marker-7";

	/// <summary>
	/// Throws synchronously, before any <see cref="Task"/> is handed back to the caller.
	/// </summary>
	private static Task ThrowBeforeTask(Exception error)
		=> throw error;

	/// <summary>
	/// Hands back an already faulted <see cref="Task"/> without ever throwing on the call itself.
	/// </summary>
	private static Task FaultedTask(Exception error)
		=> Task.FromException(error);

	/// <summary>
	/// Faults only after a real asynchronous suspension, so the exception surfaces on a
	/// continuation rather than on the calling stack.
	/// </summary>
	private async Task ThrowAfterYield(Exception error)
	{
		await Task.Delay(1, CancellationToken);
		throw error;
	}

	/// <summary>
	/// Runs <paramref name="action"/> and returns the assertion failure it raised, or <see langword="null"/>
	/// when it raised none. Anything other than <see cref="AssertFailedException"/> escapes, so a helper
	/// that lets the original exception through is reported as that exception and not as a silent pass.
	/// </summary>
	private static AssertFailedException CaptureFailure(Action action)
	{
		try
		{
			action();
			return null;
		}
		catch (AssertFailedException error)
		{
			return error;
		}
	}

	/// <summary>
	/// Asynchronous counterpart of <see cref="CaptureFailure"/>.
	/// </summary>
	private static async Task<AssertFailedException> CaptureFailureAsync(Func<Task> action)
	{
		try
		{
			await action();
			return null;
		}
		catch (AssertFailedException error)
		{
			return error;
		}
	}

	/// <summary>
	/// Asserts that a helper refused <paramref name="thrown"/> while expecting <paramref name="expected"/>,
	/// and that its failure names the two in those roles and not the other way round.
	/// </summary>
	private static void AssertRejected(AssertFailedException failure, Type expected, Exception thrown)
	{
		IsNotNull(failure, $"Expected {thrown.GetType().Name} to be refused when {expected.Name} was asked for, but the helper accepted it.");

		var thrownName = thrown.GetType().Name;

		// A helper that never ran the delegate would also fail with AssertFailedException; requiring
		// the failure to quote the exception it actually saw tells the two apart.
		Contains(expected.Name, failure.Message);
		Contains(thrownName, failure.Message);
		Contains(thrown.Message, failure.Message);

		// Both names being present would hold just as well for a report that swapped their roles, so
		// pin the roles down by position: a failure states the type it wanted before the one it got.
		var expectedAt = failure.Message.IndexOf(expected.Name, StringComparison.Ordinal);
		var thrownAt = failure.Message.IndexOf(thrownName, StringComparison.Ordinal);

		IsTrue(expectedAt < thrownAt, $"Expected {expected.Name} to be reported as the requested type and {thrownName} as the caught one, but the failure names them the other way round: {failure.Message}");
	}

	[TestMethod]
	public void Throws_AcceptsExactAndDerived_RejectsBaseAndUnrelated()
	{
		var exact = new InvalidOperationException(_msg);

		AreSame(exact, Throws<InvalidOperationException>(() => throw exact));

		// The point of Throws over ThrowsExactly: ArgumentNullException is an ArgumentException.
		var derived = new ArgumentNullException("param", _msg);

		AreSame(derived, Throws<ArgumentException>(() => throw derived));

		// The relation is one-way - a plain ArgumentException is not an ArgumentNullException.
		var baseError = new ArgumentException(_msg);

		AssertRejected(CaptureFailure(() => Throws<ArgumentNullException>(() => throw baseError)), typeof(ArgumentNullException), baseError);

		var unrelated = new InvalidOperationException(_msg);

		AssertRejected(CaptureFailure(() => Throws<ArgumentException>(() => throw unrelated)), typeof(ArgumentException), unrelated);
	}

	[TestMethod]
	public void Throws_NoException_FailsCarryingTheCustomMessage()
	{
		var invoked = false;

		var failure = CaptureFailure(() => Throws<InvalidOperationException>(() => invoked = true, _custom));

		IsNotNull(failure);
		// The helper must run the delegate, not merely fail because it never looked.
		IsTrue(invoked);
		Contains(_custom, failure.Message);
	}

	[TestMethod]
	public void ThrowsExactly_AcceptsExact_RejectsDerivedAndUnrelated()
	{
		var exact = new ArgumentNullException("param", _msg);

		AreSame(exact, ThrowsExactly<ArgumentNullException>(() => throw exact));

		// The two helpers part ways on this very instance: Throws takes it as an ArgumentException
		// because it derives from one, ThrowsExactly refuses it for exactly that reason.
		AreSame(exact, Throws<ArgumentException>(() => throw exact));
		AssertRejected(CaptureFailure(() => ThrowsExactly<ArgumentException>(() => throw exact)), typeof(ArgumentException), exact);

		var unrelated = new InvalidOperationException(_msg);

		AssertRejected(CaptureFailure(() => ThrowsExactly<ArgumentException>(() => throw unrelated)), typeof(ArgumentException), unrelated);
	}

	[TestMethod]
	public void ThrowsExactly_NoException_FailsCarryingTheCustomMessage()
	{
		var invoked = false;

		var failure = CaptureFailure(() => ThrowsExactly<InvalidOperationException>(() => invoked = true, _custom));

		IsNotNull(failure);
		IsTrue(invoked);
		Contains(_custom, failure.Message);
	}

	[TestMethod]
	public async Task ThrowsAsync_AcceptsExactAndDerivedInEveryShape_RejectsBaseAndUnrelated()
	{
		var exact = new InvalidOperationException(_msg);

		AreSame(exact, await ThrowsAsync<InvalidOperationException>(() => FaultedTask(exact)));
		// A delegate that throws on the call itself never produces a Task; the helper must still
		// report it as the awaited failure.
		AreSame(exact, await ThrowsAsync<InvalidOperationException>(() => ThrowBeforeTask(exact)));
		AreSame(exact, await ThrowsAsync<InvalidOperationException>(() => ThrowAfterYield(exact)));

		var derived = new ArgumentNullException("param", _msg);

		AreSame(derived, await ThrowsAsync<ArgumentException>(() => FaultedTask(derived)));
		AreSame(derived, await ThrowsAsync<ArgumentException>(() => ThrowBeforeTask(derived)));
		AreSame(derived, await ThrowsAsync<ArgumentException>(() => ThrowAfterYield(derived)));

		var baseError = new ArgumentException(_msg);

		AssertRejected(await CaptureFailureAsync(() => ThrowsAsync<ArgumentNullException>(() => FaultedTask(baseError))), typeof(ArgumentNullException), baseError);
		AssertRejected(await CaptureFailureAsync(() => ThrowsAsync<ArgumentNullException>(() => ThrowBeforeTask(baseError))), typeof(ArgumentNullException), baseError);
		AssertRejected(await CaptureFailureAsync(() => ThrowsAsync<ArgumentNullException>(() => ThrowAfterYield(baseError))), typeof(ArgumentNullException), baseError);

		var unrelated = new InvalidOperationException(_msg);

		AssertRejected(await CaptureFailureAsync(() => ThrowsAsync<ArgumentException>(() => FaultedTask(unrelated))), typeof(ArgumentException), unrelated);
		AssertRejected(await CaptureFailureAsync(() => ThrowsAsync<ArgumentException>(() => ThrowBeforeTask(unrelated))), typeof(ArgumentException), unrelated);
		AssertRejected(await CaptureFailureAsync(() => ThrowsAsync<ArgumentException>(() => ThrowAfterYield(unrelated))), typeof(ArgumentException), unrelated);
	}

	[TestMethod]
	public async Task ThrowsAsync_NoException_FailsCarryingTheCustomMessage()
	{
		var completedInvoked = false;

		// An already completed Task never suspends, so the helper takes a different path than the
		// yielding delegate below.
		var completedFailure = await CaptureFailureAsync(() => ThrowsAsync<InvalidOperationException>(() =>
		{
			completedInvoked = true;
			return Task.CompletedTask;
		}, _custom));

		IsNotNull(completedFailure);
		IsTrue(completedInvoked);
		Contains(_custom, completedFailure.Message);

		var yieldedInvoked = false;

		var yieldedFailure = await CaptureFailureAsync(() => ThrowsAsync<InvalidOperationException>(async () =>
		{
			await Task.Delay(1, CancellationToken);
			yieldedInvoked = true;
		}));

		IsNotNull(yieldedFailure);
		IsTrue(yieldedInvoked);
	}

	[TestMethod]
	public async Task ThrowsExactlyAsync_AcceptsExactInEveryShape_RejectsDerivedAndUnrelated()
	{
		var exact = new ArgumentNullException("param", _msg);

		AreSame(exact, await ThrowsExactlyAsync<ArgumentNullException>(() => FaultedTask(exact)));
		AreSame(exact, await ThrowsExactlyAsync<ArgumentNullException>(() => ThrowBeforeTask(exact)));
		AreSame(exact, await ThrowsExactlyAsync<ArgumentNullException>(() => ThrowAfterYield(exact)));

		// The two helpers part ways on this very instance: ThrowsAsync takes it as an ArgumentException
		// because it derives from one, ThrowsExactlyAsync refuses it for exactly that reason.
		AreSame(exact, await ThrowsAsync<ArgumentException>(() => FaultedTask(exact)));
		AssertRejected(await CaptureFailureAsync(() => ThrowsExactlyAsync<ArgumentException>(() => FaultedTask(exact))), typeof(ArgumentException), exact);
		AssertRejected(await CaptureFailureAsync(() => ThrowsExactlyAsync<ArgumentException>(() => ThrowBeforeTask(exact))), typeof(ArgumentException), exact);
		AssertRejected(await CaptureFailureAsync(() => ThrowsExactlyAsync<ArgumentException>(() => ThrowAfterYield(exact))), typeof(ArgumentException), exact);

		var unrelated = new InvalidOperationException(_msg);

		AssertRejected(await CaptureFailureAsync(() => ThrowsExactlyAsync<ArgumentException>(() => FaultedTask(unrelated))), typeof(ArgumentException), unrelated);
		AssertRejected(await CaptureFailureAsync(() => ThrowsExactlyAsync<ArgumentException>(() => ThrowBeforeTask(unrelated))), typeof(ArgumentException), unrelated);
		AssertRejected(await CaptureFailureAsync(() => ThrowsExactlyAsync<ArgumentException>(() => ThrowAfterYield(unrelated))), typeof(ArgumentException), unrelated);
	}

	[TestMethod]
	public async Task ThrowsExactlyAsync_NoException_FailsCarryingTheCustomMessage()
	{
		var completedInvoked = false;

		var completedFailure = await CaptureFailureAsync(() => ThrowsExactlyAsync<InvalidOperationException>(() =>
		{
			completedInvoked = true;
			return Task.CompletedTask;
		}, _custom));

		IsNotNull(completedFailure);
		IsTrue(completedInvoked);
		Contains(_custom, completedFailure.Message);

		var yieldedInvoked = false;

		var yieldedFailure = await CaptureFailureAsync(() => ThrowsExactlyAsync<InvalidOperationException>(async () =>
		{
			await Task.Delay(1, CancellationToken);
			yieldedInvoked = true;
		}));

		IsNotNull(yieldedFailure);
		IsTrue(yieldedInvoked);
	}
}
