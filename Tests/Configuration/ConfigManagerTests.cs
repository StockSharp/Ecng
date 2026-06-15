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

	private interface ITryFallbackService
	{
	}

	private sealed class TryFallbackService : ITryFallbackService
	{
	}

	private interface ITryMissingService
	{
	}

	[TestMethod]
	public void TryGetService_ServiceFallback_ResolvesViaFallback()
	{
		var expected = new TryFallbackService();

		Func<Type, string, object> handler = (type, _) =>
			type == typeof(ITryFallbackService) ? expected : null;

		ConfigManager.ServiceFallback += handler;

		try
		{
			// TryGetService must consult the fallback like GetService does, so a
			// service available only through the fallback is returned (not default).
			ConfigManager.TryGetService<ITryFallbackService>().AssertSame(expected);
		}
		finally
		{
			ConfigManager.ServiceFallback -= handler;
		}
	}

	[TestMethod]
	public void TryGetService_NoFallback_ReturnsDefault()
	{
		// With no fallback able to construct it, an unregistered service stays default
		// (backward-compatible: TryGetService never throws and never invents a service).
		ConfigManager.TryGetService<ITryMissingService>().AssertNull();
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
	/// Regression test for <see cref="ConfigManager.SubscribeOnRegister{T}"/> reentrancy: ensures a
	/// subscriber that re-subscribes for the same type during notification is safe and registration
	/// completes without throwing, because the subscriber list is snapshotted before callbacks run
	/// (Configuration\ConfigManager.cs:219). (Was: RaiseServiceRegistered enumerated the live
	/// subscriber list, so re-subscribing during notification threw InvalidOperationException.)
	/// </summary>
	[TestMethod]
	public void SubscribeOnRegister_ReentrantSubscribe_DoesNotThrow()
	{
		var service = new ReentrantService();
		var name = $"{nameof(SubscribeOnRegister_ReentrantSubscribe_DoesNotThrow)}_{Guid.NewGuid():N}";

		// First subscriber re-subscribes for the same type while being notified,
		// adding to the subscriber list during a RaiseServiceRegistered notification.
		ConfigManager.SubscribeOnRegister<IReentrantService>(_ =>
			ConfigManager.SubscribeOnRegister<IReentrantService>(__ => { }));

		// Triggers RaiseServiceRegistered for IReentrantService.
		// RaiseServiceRegistered snapshots the subscriber list before invoking callbacks,
		// so the reentrant subscribe above is safe and this completes cleanly.
		ConfigManager.RegisterService<IReentrantService>(name, service);

		ConfigManager.GetService<IReentrantService>(name).AssertSame(service);
	}
}
