namespace Ecng.Net
{
	using System;

	[AttributeUsage(AttributeTargets.Parameter)]
	public abstract class ParamConverterAttribute : Attribute
	{
		protected ParamConverterAttribute(Type sourceType)
		{
			SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
		}

		internal void Init(Type destType)
		{
			DestType = destType ?? throw new ArgumentNullException(nameof(destType));
		}

		public Type SourceType { get; }
		public Type DestType { get; private set; }

		protected internal abstract object Convert(object sourceValue);
	}

	class DefaultParamConverterAttribute : ParamConverterAttribute
	{
		public DefaultParamConverterAttribute(Type sourceType)
			: base(sourceType)
		{
		}

		protected internal override object Convert(object sourceValue)
		{
			return sourceValue;
		}
	}
}