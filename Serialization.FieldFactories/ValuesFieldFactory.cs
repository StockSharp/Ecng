namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[Serializable]
	public class ValuesFieldFactory<I, S> : FieldFactory<I, S>
	{
		#region ValuesFieldFactory.ctor()

		public ValuesFieldFactory(PairSet<I, S> values, NullableEx<I> defaultValue, Field field, int order)
			: base(field, order)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			Values = values;
			DefaultValue = defaultValue;
		}

		#endregion

		public PairSet<I, S> Values { get; private set; }
		public NullableEx<I> DefaultValue { get; private set; }

		#region FieldFactory<I, S> Members

		protected override I OnCreateInstance(ISerializer serializer, S source)
		{
			if (!Values.ContainsValue(source))
			{
				if (DefaultValue.HasValue)
					return DefaultValue.Value;
				else
					throw new ArgumentException("source");
			}
			else
				return Values[source];
		}

		protected override S OnCreateSource(ISerializer serializer, I instance)
		{
			S source;

			if (Values.TryGetValue(instance, out source))
				return source;
			else
				throw new ArgumentException("instance");
		}

		#endregion

		#region Serializable Members

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			source.Add(new SerializationItem(new VoidField<byte[]>("values"), serializer.GetSerializer<PairSet<I, S>>().Serialize(Values)));
			source.Add(new SerializationItem(new VoidField<NullableEx<I>>("defaultValue"), DefaultValue));
			base.Serialize(serializer, fields, source);
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Values = serializer.GetSerializer<PairSet<I, S>>().Deserialize((byte[])source["values"].Value);
			DefaultValue = (NullableEx<I>)source["defaultValue"].Value;
			base.Deserialize(serializer, fields, source);
		}

		#endregion
	}

	[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types | AttributeTargets.Enum, AllowMultiple = true)]
	public class ValueAttribute : Attribute
	{
		#region ValueAttribute.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="ValueAttribute"/> class.
		/// </summary>
		/// <param name="sourceValue">The source value.</param>
		public ValueAttribute(object sourceValue)
		{
			SourceValue = sourceValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ValueAttribute"/> class.
		/// </summary>
		/// <param name="instanceValue">The instance value.</param>
		/// <param name="sourceValue">The source value.</param>
		public ValueAttribute(object instanceValue, object sourceValue)
		{
			InstanceValue = instanceValue;
			SourceValue = sourceValue;
		}

		#endregion

		public object InstanceValue { get; private set; }
		public object SourceValue { get; private set; }
	}

	[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types | AttributeTargets.Enum)]
	public class ValuesAttribute : FieldFactoryAttribute
	{
		#region Private Fields

		private static readonly MethodInfo _createFactoryMethod = typeof(ValuesAttribute).GetMember<MethodInfo>("nCreateFactory");

		private readonly Type _valueType;

		#endregion

		#region ValuesAttribute.ctor()

		public ValuesAttribute(Type valueType)
		{
			if (valueType == null)
				throw new ArgumentNullException("valueType");

			_valueType = valueType;
		}

		#endregion

		#region DefaultValue

		public object DefaultValue { get; set; }

		#endregion

		#region FieldFactoryAttribute Members

		public override FieldFactory CreateFactory(Field field)
		{
			return this.GetValue<ValuesAttribute, Field, FieldFactory>(_createFactoryMethod.Make(field.Type, _valueType), field);
		}

		#endregion

		#region CreateFactory

		private ValuesFieldFactory<I, S> nCreateFactory<I, S>(Field field)
		{
			var values = new PairSet<I, S>();

			foreach (var valueAttr in field.Type.GetAttributes<ValueAttribute>())
				values.Add(valueAttr.InstanceValue.To<I>(), (S)valueAttr.SourceValue);

			if (field.Type.IsEnum())
			{
				foreach (var enumField in field.Type.GetMembers<FieldInfo>())
				{
					var valueAttr = enumField.GetAttribute<ValueAttribute>();
					if (valueAttr != null)
						values.Add((I)enumField.GetValue(null), (S)valueAttr.SourceValue);
				}
			}

			foreach (var valueAttr in field.Member.GetAttributes<ValueAttribute>())
				values[valueAttr.InstanceValue.To<I>()] = (S)valueAttr.SourceValue;

			if (values.IsEmpty())
				throw new ArgumentException("Member '{0}' has empty value set.".Put(field.Name), "field");

			var defaultValue = new NullableEx<I>();

			if (DefaultValue != null)
			{
				defaultValue.Value = DefaultValue.To<I>();

				if (values.ContainsKey(defaultValue.Value))
					throw new ArgumentException("Member '{0}' has incorrect default value '{1}'.".Put(field.Name, defaultValue.Value), "field");
			}

			return new ValuesFieldFactory<I, S>(values, defaultValue, field, Order);
		}

		#endregion
	}
}