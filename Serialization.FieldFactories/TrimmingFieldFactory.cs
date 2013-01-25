namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[Serializable]
	[Flags]
	public enum TrimOptions
	{
		Start = 1,
		End = Start << 1,
		Both = Start | End,
	}

	[Serializable]
	public class TrimmingFieldFactory : FieldFactory<string, string>
	{
		#region TrimmingFieldFactory.ctor()

		public TrimmingFieldFactory(TrimOptions options, Field field, int order)
			: base(field, order)
		{
			Options = options;
		}

		#endregion

		#region Options

		public TrimOptions Options { get; private set; }

		#endregion

		#region FieldFactory<string, string> Members

		protected override string OnCreateInstance(ISerializer serializer, string source)
		{
			switch (Options)
			{
				case TrimOptions.Start:
					return source.TrimStart();
				case TrimOptions.End:
					return source.TrimEnd();
				case TrimOptions.Both:
					return source.Trim();
				default:
					throw new InvalidOperationException();
			}
		}

		protected override string OnCreateSource(ISerializer serializer, string instance)
		{
			return instance;
		}

		#endregion

		#region Serializable Members

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			source.Add(new SerializationItem(new VoidField<TrimOptions>("options"), Options));
			base.Serialize(serializer, fields, source);
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Options = source["options"].Value.To<TrimOptions>();
			base.Deserialize(serializer, fields, source);
		}

		#endregion
	}

	[AttributeUsage(ReflectionHelper.Members)]
	public sealed class TrimAttribute : FieldFactoryAttribute
	{
		public TrimOptions Options { get; set; }

		#region FieldFactoryAttribute Members

		public override FieldFactory CreateFactory(Field field)
		{
			return new TrimmingFieldFactory(Options, field, Order);
		}

		#endregion
	}
}