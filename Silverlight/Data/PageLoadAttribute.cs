namespace Ecng.Data
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Reflection;

	public class PageLoadAttribute : ReflectionFieldFactoryAttribute
	{
		public Type ListType { get; set; }
		public bool BulkLoad { get; set; }

		protected override Type GetFactoryType(Field field)
		{
			return typeof(PrimitiveFieldFactory<,>).Make(field.Type, typeof(VoidType));
		}
	}
}