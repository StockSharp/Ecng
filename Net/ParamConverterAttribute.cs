namespace Ecng.Net
{
	using System;

	[AttributeUsage(AttributeTargets.Parameter)]
	public abstract class ParamConverterAttribute : Attribute
	{
		protected ParamConverterAttribute(Type sourceType)
		{
			if (sourceType == null)
				throw new ArgumentNullException(nameof(sourceType));

			SourceType = sourceType;
		}

		internal void Init(Type destType)
		{
			if (destType == null)
				throw new ArgumentNullException(nameof(destType));

			DestType = destType;
		}

		public Type SourceType { get; private set; }
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