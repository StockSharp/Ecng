namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Reflection;
	using System.Reflection.Emit;

	#endregion

	public class EventGenerator : BaseGenerator<EventBuilder>
	{
		#region Private Fields

		private readonly TypeGenerator _typeGenerator;

		private readonly string _eventName;
		private readonly Type _eventType;

		private MethodGenerator _addMethod;
		private MethodGenerator _removeMethod;

		private FieldGenerator _eventField;

		#endregion

		#region EventGenerator.ctor()

		internal EventGenerator(EventBuilder eventBuilder, string eventName, Type eventType, TypeGenerator typeGenerator)
			: base(eventBuilder)
		{
			_eventName = eventName;
			_eventType = eventType;

			_typeGenerator = typeGenerator;
		}

		#endregion

		public const MethodAttributes DefaultMethodAttrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;

		#region CreateAddMethod

		public MethodGenerator CreateAddMethod()
		{
			return CreateAddMethod(DefaultMethodAttrs);
		}

		public MethodGenerator CreateAddMethod(MethodAttributes methodAttrs)
		{
			lock (this)
			{
				if (_addMethod == null)
				{
					_addMethod = _typeGenerator.CreateMethod("add_" + _eventName, methodAttrs, typeof(void), _eventType);
					Builder.SetAddOnMethod((MethodBuilder)_addMethod.Builder);
				}
			}

			return _addMethod;
		}

		#endregion

		#region CreateRemoveMethod

		public MethodGenerator CreateRemoveMethod()
		{
			return CreateRemoveMethod(DefaultMethodAttrs);
		}

		public MethodGenerator CreateRemoveMethod(MethodAttributes methodAttrs)
		{
			lock (this)
			{
				if (_removeMethod == null)
				{
					_removeMethod = _typeGenerator.CreateMethod("remove_" + _eventName, methodAttrs, typeof(void), _eventType);
					Builder.SetAddOnMethod((MethodBuilder)_removeMethod.Builder);
				}
			}

			return _removeMethod;
		}

		#endregion

		#region CreateEventField

		public FieldGenerator CreateEventField()
		{
			lock (this)
			{
				if (_eventField == null)
					_eventField = _typeGenerator.CreateField("_" + _eventName, _eventType, FieldAttributes.Private);
			}

			return _eventField;
		}

		#endregion

		#region CreateDefaultImp

		public void CreateDefaultImp()
		{
			CreateDefaultImp(DefaultMethodAttrs);
		}

		public void CreateDefaultImp(MethodAttributes methodAttrs)
		{
			var eventField = CreateEventField();

			var addMethod = CreateAddMethod(methodAttrs);
			addMethod
					.ldarg_0()
					.ldarg_0()
					.GetMember(false, eventField.Builder)
					.ldarg_1()
					.call(typeof(Delegate).GetMember<MethodInfo>("Combine", new[] { typeof(Delegate), typeof(Delegate) }))
					.castclass(_eventType)
					.stfld(eventField)
					.ret();

			var removeMethod = CreateRemoveMethod(methodAttrs);
			removeMethod
					.ldarg_0()
					.ldarg_0()
					.GetMember(false, eventField.Builder)
					.ldarg_1()
					.call(typeof(Delegate).GetMember<MethodInfo>("Remove"))
					.castclass(_eventType)
					.stfld(eventField)
					.ret();
		}

		#endregion
	}
}