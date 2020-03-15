namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;
#if !NETCOREAPP && !NETSTANDARD
	using System.Runtime.Serialization.Formatters.Soap;
#endif

	using Ecng.Common;

	[Serializable]
	public class FormatterFieldFactory<I, F> : FieldFactory<I, Stream>
		where F : IFormatter, new()
	{
		public FormatterFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		#region FieldFactory<I, byte[]> Members

		protected override I OnCreateInstance(ISerializer serializer, Stream source)
		{
			if (source.Length == 0)
				return default;

			return (I)new F().Deserialize(source);
		}

		protected override Stream OnCreateSource(ISerializer serializer, I instance)
		{
			var stream = new MemoryStream();
			new F().Serialize(stream, instance);
			stream.Position = 0;
			return stream;
		}

		#endregion
	}

	public abstract class FormatterAttribute : ReflectionFieldFactoryAttribute
	{
		#region ReflectionFieldFactoryAttribute Members

		protected override Type GetFactoryType(Field field)
		{
			return typeof(FormatterFieldFactory<,>).Make(field.Type, GetFormatterType());
		}

		#endregion

		protected abstract Type GetFormatterType();
	}

	public class BinaryFormatterAttribute : FormatterAttribute
	{
		protected override Type GetFormatterType()
		{
			return typeof(BinaryFormatter);
		}
	}

#if !NETCOREAPP && !NETSTANDARD
	public class XmlFormatterAttribute : FormatterAttribute
	{
		protected override Type GetFormatterType()
		{
			return typeof(SoapFormatter);
		}
	}
#endif
}