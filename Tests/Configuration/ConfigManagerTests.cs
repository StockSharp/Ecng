namespace Ecng.Tests.Configuration;

using Ecng.Configuration;

[TestClass]
public class ConfigManagerTests : BaseTestClass
{
	private interface IFallbackService
	{
	}

	private sealed class FallbackService : IFallbackService
	{
	}

	[TestMethod]
	public void GetService_ServiceFallback_UsesFirstNonNullHandler()
	{
		var expected = new FallbackService();
		var name = $"{nameof(GetService_ServiceFallback_UsesFirstNonNullHandler)}_{Guid.NewGuid():N}";

		Func<Type, string, object> first = (type, serviceName) =>
			type == typeof(IFallbackService) && serviceName == name ? expected : null;

		Func<Type, string, object> second = (_, _) => null;

		ConfigManager.ServiceFallback += first;
		ConfigManager.ServiceFallback += second;

		try
		{
			ConfigManager.GetService<IFallbackService>(name).AssertSame(expected);
		}
		finally
		{
			ConfigManager.ServiceFallback -= second;
			ConfigManager.ServiceFallback -= first;
		}
	}

	// Per-test marker types keep ConfigManager's static, type-keyed registries
	// isolated from other tests (the registry is process-wide static state).
	private interface IReentrantService
	{
	}

	private sealed class ReentrantService : IReentrantService
	{
	}

	/// <summary>
	/// BUG: <see cref="ConfigManager.RaiseServiceRegistered"/> enumerates the live subscriber
	/// list while a subscriber callback may mutate it via <see cref="ConfigManager.SubscribeOnRegister{T}"/>
	/// (Configuration\ConfigManager.cs:209,219). A subscriber that re-subscribes for the same type
	/// during notification adds to the very list being iterated, invalidating the enumerator.
	/// Expected: re-subscribing during notification is safe (a snapshot is iterated), so registration
	/// completes without throwing. Actual: the foreach throws InvalidOperationException
	/// ("Collection was modified"), breaking the documented thread-safe contract.
	/// </summary>
	[TestMethod]
	public void SubscribeOnRegister_ReentrantSubscribe_DoesNotThrow()
	{
		var service = new ReentrantService();
		var name = $"{nameof(SubscribeOnRegister_ReentrantSubscribe_DoesNotThrow)}_{Guid.NewGuid():N}";

		// First subscriber re-subscribes for the same type while being notified,
		// mutating the list that RaiseServiceRegistered is currently enumerating.
		ConfigManager.SubscribeOnRegister<IReentrantService>(_ =>
			ConfigManager.SubscribeOnRegister<IReentrantService>(__ => { }));

		// Triggers RaiseServiceRegistered for IReentrantService.
		// With the bug present this throws InvalidOperationException; the fix snapshots
		// the subscriber list before invoking callbacks, so this must complete cleanly.
		ConfigManager.RegisterService<IReentrantService>(name, service);

		ConfigManager.GetService<IReentrantService>(name).AssertSame(service);
	}
}
