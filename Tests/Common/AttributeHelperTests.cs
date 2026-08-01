namespace Ecng.Tests.Common;

using System.ComponentModel;
using System.Runtime.Loader;

#pragma warning disable CS0612 // Type or member is obsolete

[TestClass]
public class AttributeHelperTests : BaseTestClass
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	private class CustomAttribute : Attribute
	{
	}

	[Obsolete]
	[Browsable(false)]
	private class AttrClass { }

	private sealed class ModulelessMemberInfo : MemberInfo
	{
		private readonly Type _declaringType;
		private readonly bool _notSupported;

		public ModulelessMemberInfo(Type declaringType, bool notSupported = false)
		{
			_declaringType = declaringType;
			_notSupported = notSupported;
		}

		public int AttributeRequests { get; private set; }

		public override Type DeclaringType => _declaringType;
		public override MemberTypes MemberType => MemberTypes.Custom;
		public override string Name => nameof(ModulelessMemberInfo);
		public override Type ReflectedType => _declaringType;
		public override Module Module
			=> _notSupported ? throw new NotSupportedException() : throw new NotImplementedException();

		public override object[] GetCustomAttributes(bool inherit) => [new ObsoleteAttribute()];

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			AttributeRequests++;
			return attributeType == typeof(ObsoleteAttribute) ? [new ObsoleteAttribute()] : [];
		}

		public override bool IsDefined(Type attributeType, bool inherit)
			=> attributeType == typeof(ObsoleteAttribute);
	}

	[Custom]
	private class BaseClass { }

	private class DerivedClass : BaseClass { }

	[TestMethod]
	[DoNotParallelize]
	public void GetAttributeCaching()
	{
		var previous = AttributeHelper.CacheEnabled;
		AttributeHelper.ClearCache();

		try
		{
			AttributeHelper.CacheEnabled = true;
			var type = typeof(AttrClass);
			var a1 = type.GetAttribute<ObsoleteAttribute>();
			var a2 = type.GetAttribute<ObsoleteAttribute>();
			a1.AssertNotNull();
			a1.AssertSame(a2);
			AttributeHelper.CacheEnabled = false;
			AttributeHelper.ClearCache();
			a1 = type.GetAttribute<ObsoleteAttribute>();
			a2 = type.GetAttribute<ObsoleteAttribute>();
			a1.AssertNotSame(a2);
		}
		finally
		{
			AttributeHelper.CacheEnabled = previous;
			AttributeHelper.ClearCache();
		}
	}

	[TestMethod]
	[DoNotParallelize]
	public void CachedAttributeDoesNotPreventCollectibleContextUnload()
	{
		var previous = AttributeHelper.CacheEnabled;
		AttributeHelper.ClearCache();

		try
		{
			AttributeHelper.CacheEnabled = true;
			var contextReference = PopulateCacheFromCollectibleContext();

			for (var i = 0; i < 10 && contextReference.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}

			contextReference.IsAlive.AssertFalse(
				"Attribute caching must not keep a collectible assembly load context alive.");
		}
		finally
		{
			AttributeHelper.CacheEnabled = previous;
			AttributeHelper.ClearCache();
		}
	}

	[TestMethod]
	[DoNotParallelize]
	public void CacheSupportsMemberInfoWithoutModule()
	{
		var previous = AttributeHelper.CacheEnabled;
		AttributeHelper.ClearCache();

		try
		{
			AttributeHelper.CacheEnabled = true;
			var provider = new ModulelessMemberInfo(typeof(ModulelessMemberInfo));

			var first = provider.GetAttribute<ObsoleteAttribute>();
			var second = provider.GetAttribute<ObsoleteAttribute>();

			first.AssertNotNull();
			first.AssertSame(second);
			provider.AttributeRequests.AssertEqual(1);
		}
		finally
		{
			AttributeHelper.CacheEnabled = previous;
			AttributeHelper.ClearCache();
		}
	}

	[TestMethod]
	[DoNotParallelize]
	public void CacheSkipsMemberInfoWithUnknownAssembly()
	{
		var previous = AttributeHelper.CacheEnabled;
		AttributeHelper.ClearCache();

		try
		{
			AttributeHelper.CacheEnabled = true;
			var provider = new ModulelessMemberInfo(null, true);

			var first = provider.GetAttribute<ObsoleteAttribute>();
			var second = provider.GetAttribute<ObsoleteAttribute>();

			first.AssertNotNull();
			first.AssertNotSame(second);
			provider.AttributeRequests.AssertEqual(2);
		}
		finally
		{
			AttributeHelper.CacheEnabled = previous;
			AttributeHelper.ClearCache();
		}
	}

	[TestMethod]
	public void AttributeQueries()
	{
		var type = typeof(AttrClass);
		type.GetAttribute<ObsoleteAttribute>().AssertNotNull();
		type.GetAttributes<Attribute>().Count().AssertEqual(2);
		type.GetAttributes().Count().AssertEqual(2);
		type.IsObsolete().AssertTrue();
		type.IsBrowsable().AssertFalse();

		typeof(DerivedClass).IsObsolete().AssertFalse();
		typeof(DerivedClass).IsBrowsable().AssertTrue();
	}

	[TestMethod]
	public void InheritSearch()
	{
		typeof(DerivedClass).GetAttribute<CustomAttribute>(false).AssertNull();
		typeof(DerivedClass).GetAttribute<CustomAttribute>(true).AssertNotNull();
	}

	[TestMethod]
	public void NullProvider()
	{
		ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttribute<ObsoleteAttribute>(null));
		ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttributes<ObsoleteAttribute>(null).ToArray());
		ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttributes(null).ToArray());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference PopulateCacheFromCollectibleContext()
	{
		var context = new AssemblyLoadContext(nameof(CachedAttributeDoesNotPreventCollectibleContextUnload), true);
		var assembly = context.LoadFromAssemblyPath(typeof(AttributeHelperTests).Assembly.Location);
		var type = assembly.GetType(typeof(AttrClass).FullName, true);

		type.GetAttribute<ObsoleteAttribute>().AssertNotNull();
		context.Unload();

		return new(context);
	}
}
