namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;

	[Serializable]
	public class ValidationFieldFactory<T> : FieldFactory<T, T>
	{
		#region ValidationFieldFactory.ctor()

		public ValidationFieldFactory(BaseValidator<T> validator, Field field, int order)
			: base(field, order)
		{
			if (validator == null)
				throw new ArgumentNullException(nameof(validator));

			Validator = validator;
		}

		#endregion

		public BaseValidator<T> Validator { get; }

		#region FieldFactory Members

		protected override T OnCreateInstance(ISerializer serializer, T source)
		{
			Validate(source);
			return source;
		}

		protected override T OnCreateSource(ISerializer serializer, T instance)
		{
			Validate(instance);
			return instance;
		}

		#endregion

		private void Validate(T value)
		{
			try
			{
				Validator.Validate(value);
			}
			catch (Exception e)
			{
				throw new ArgumentException("Value '{0}' of field '{1}' of schema '{2}' cannot process validating.".Put(value, Field.Name, Field.Schema.EntityType), e);
			}
		}
	}

	[AttributeUsage(ReflectionHelper.Members)]
	public class ValidationAttribute : FieldFactoryAttribute
	{
		#region FieldFactoryAttribute Members

		public override FieldFactory CreateFactory(Field field)
		{
			var attr = field.Member.GetAttribute<BaseValidatorAttribute>();
			
			if (attr == null)
				throw new ArgumentNullException(nameof(field));

			return typeof(ValidationFieldFactory<>).Make(field.Type).CreateInstance<FieldFactory>(attr.CreateValidator(field.Type), field, Order);
		}

		#endregion
	}
}