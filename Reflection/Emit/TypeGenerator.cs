namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Linq;

	using Ecng.Common;

	#endregion

	public sealed class TypeGenerator : BaseGenerator<TypeBuilder>
	{
		private Action<Type> _typeCompiled;

		#region TypeGenerator.ctor()

		internal TypeGenerator(TypeBuilder builder, Type[] baseTypes, Action<Type> typeCompiled)
			: base(builder)
		{
			if (baseTypes is null)
				throw new ArgumentNullException(nameof(baseTypes));

			Type baseType = null;

			var interfaceTypes = new List<Type>();

			foreach (var type in baseTypes)
			{
				if (type.IsInterface)
					interfaceTypes.Add(type);
				else
				{
					if (baseType != null)
						throw new ArgumentException(nameof(baseTypes));

					baseType = type;
				}
			}

			BaseType = baseType;
			Interfaces = interfaceTypes;
			_typeCompiled = typeCompiled ?? throw new ArgumentNullException(nameof(typeCompiled));
			//_typeBuilder = module.DefineType(typeName, attrs);

			//foreach (Type interfaceType in interfaceTypes)
			//	_typeBuilder.AddInterfaceImplementation(interfaceType);
		}

		#endregion

		#region BaseType

		public Type BaseType { get; }

		#endregion

		#region Interfaces

		public IEnumerable<Type> Interfaces { get; }

		#endregion

		#region Methods

		private readonly List<MethodGenerator> _methods = new();

		public IEnumerable<MethodGenerator> Methods => _methods;

		#endregion

		#region Ctors

		private readonly List<MethodGenerator> _ctors = new();

		public IEnumerable<MethodGenerator> Ctors => _ctors;

		#endregion

		#region Properties

		private readonly List<PropertyGenerator> _properties = new();

		public IEnumerable<PropertyGenerator> Properties => _properties;

		#endregion

		#region Fields

		private readonly List<FieldGenerator> _fields = new();

		public IEnumerable<FieldGenerator> Fields => _fields;

		#endregion

		#region Events

		private readonly List<EventGenerator> _events = new();

		public IEnumerable<EventGenerator> Events => _events;

		#endregion

		#region CreateGenericParameters

		private IEnumerable<GenericArgGenerator> _genericArgs;

		public IEnumerable<GenericArgGenerator> CreateGenericParameters(params string[] names)
		{
			_genericArgs = Builder.DefineGenericParameters(names).Select(builder => new GenericArgGenerator(builder)).ToArray();
			return _genericArgs;
		}

		#endregion

		#region CreateMethod

		public MethodGenerator CreateMethod(string methodName, MethodAttributes attrs, Type returnType, params Type[] types)
		{
			var generator = new MethodGenerator(this, Builder.DefineMethod(methodName, attrs, returnType, types));

			_methods.Add(generator);
			return generator;
		}

		#endregion

		//#region CreateProperty

		//public PropertyGenerator CreateProperty(string propertyName, MethodAttributes methodAttrs, PropertyAttributes attrs, Type returnType, params Type[] types)
		//{
		//    PropertyGenerator property = new PropertyGenerator(_typeBuilder.DefineProperty(propertyName, attrs, returnType, types), methodAttrs, types, this);
		//    _properties.Add(property);
		//    return property;
		//}

		//#endregion

		#region CreateConstructor

		public MethodGenerator CreateConstructor(MethodAttributes attrs, params Type[] args)
		{
			var ctor = new MethodGenerator(this, Builder.DefineConstructor(attrs, CallingConventions.Standard, args));
			_ctors.Add(ctor);
			return ctor;
		}

		public MethodGenerator CreateConstructor()
		{
			var ctor = new MethodGenerator(null, Builder.DefineDefaultConstructor(MethodAttributes.Public));
			_ctors.Add(ctor);
			return ctor;
		}

		#endregion

		#region CreateField

		public FieldGenerator CreateField(string fieldName, Type fieldType, FieldAttributes attrs)
		{
			var field = new FieldGenerator(Builder.DefineField(fieldName, fieldType, attrs));
			_fields.Add(field);
			return field;
		}

		#endregion

		#region CreateEvent

		public EventGenerator CreateEvent(string eventName, Type eventType, EventAttributes attrs)
		{
			var evt = new EventGenerator(Builder.DefineEvent(eventName, attrs, eventType), eventName, eventType, this);
			_events.Add(evt);
			return evt;
		}

		#endregion

		//#region CreateCustomAttribute

		//public void CreateCustomAttribute(ConstructorInfo constructor, object[] args)
		//{
		//    _typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, args));
		//}

		//#endregion

		#region CompileType

		public Type CompileType()
		{
			if (BaseType != null)
				Builder.SetParent(TryMakeGenericType(BaseType));

			foreach (Type @interface in Interfaces)
				Builder.AddInterfaceImplementation(TryMakeGenericType(@interface));

			Type retVal = Builder.CreateTypeInfo();

			_typeCompiled?.Invoke(retVal);

			return retVal;
		}

		#endregion

		private Type TryMakeGenericType(Type type)
		{
			if (type.IsGenericTypeDefinition)
			{
				var names = type.GetGenericArguments().Select(arg => arg.Name);

				return type.Make(_genericArgs.Where(arg => names.Contains(arg.Builder.Name)).Select<GenericArgGenerator, Type>(arg => arg.Builder));
			}
			else
				return type;
		}
	}
}