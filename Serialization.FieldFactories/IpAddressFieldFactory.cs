namespace Ecng.Serialization
{
	using System;
	using System.Net;

	public class IpAddressFieldFactory<TSource> : PrimitiveFieldFactory<IPAddress, TSource>
	{
		public IpAddressFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class IpAddressAttribute : ReflectionFieldFactoryAttribute
	{
		public bool AsString { get; set; }

		protected override Type GetFactoryType(Field field)
		{
			return AsString ? typeof(IpAddressFieldFactory<string>) : typeof(IpAddressFieldFactory<long>);
		}
	}
}