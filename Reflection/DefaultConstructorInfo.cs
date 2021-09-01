namespace Ecng.Reflection
{
	#region Using Directives

	using System;
	using System.Globalization;
	using System.Reflection;

	using Ecng.Common;

	#endregion

	public class DefaultConstructorInfo : ConstructorInfo
	{
		#region Private Fields

		private readonly Type _type;

		#endregion

		#region DefaultConstructorInfo.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Ecng.Reflection.DefaultConstructorInfo"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public DefaultConstructorInfo(Type type)
		{
			_type = type;
		}

		#endregion

		#region ConstructorInfo Member

		/// <summary>
		/// Gets the attributes associated with this method.
		/// </summary>
		/// <value></value>
		/// <returns>One of the <see cref="T:System.Reflection.MethodAttributes"></see> values.</returns>
		public override MethodAttributes Attributes => MethodAttributes.Public;

		/// <summary>
		/// Gets the class that declares this member.
		/// </summary>
		/// <value></value>
		/// <returns>The Type object for the class that declares this member.</returns>
		public override Type DeclaringType => _type;

		/// <summary>
		/// Gets the name of the current member.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.String"></see> containing the name of this member.</returns>
		public override string Name => ConstructorInfo.ConstructorName;

		/// <summary>
		/// Gets a handle to the internal metadata representation of a method.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.RuntimeMethodHandle"></see> object.</returns>
		public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

		/// <summary>
		/// Gets the class object that was used to obtain this instance of <see cref="MemberInfo"/>.
		/// </summary>
		/// <value></value>
		/// <returns>The Type object through which this <see cref="MemberInfo"/> object was obtained.</returns>
		public override Type ReflectedType => _type;

		/// <summary>
		/// When implemented in a derived class, invokes the constructor reflected by this <see cref="ConstructorInfo"/> with the specified arguments, under the constraints of the specified Binder.
		/// </summary>
		/// <param name="invokeAttr">One of the <see cref="BindingFlags"/> values that specifies the type of binding.</param>
		/// <param name="binder">A Binder that defines a set of properties and enables the binding, coercion of argument types, and invocation of members using reflection. If binder is null, then Binder.DefaultBinding is used.</param>
		/// <param name="parameters">An array of type Object used to match the number, order and type of the parameters for this constructor, under the constraints of binder. If this constructor does not require parameters, pass an array with zero elements, as in Object[] parameters = new Object[0]. Any object in this array that is not explicitly initialized with a value will contain the default value for that object type. For reference-type elements, this value is null. For value-type elements, this value is 0, 0.0, or false, depending on the specific element type.</param>
		/// <param name="culture">A <see cref="T:System.Globalization.CultureInfo"></see> used to govern the coercion of types. If this is null, the <see cref="T:System.Globalization.CultureInfo"></see> for the current thread is used.</param>
		/// <returns>
		/// An instance of the class associated with the constructor.
		/// </returns>
		/// <exception cref="T:System.Reflection.TargetInvocationException">The invoked constructor throws an exception. </exception>
		/// <exception cref="T:System.Reflection.TargetParameterCountException">An incorrect number of parameters was passed. </exception>
		/// <exception cref="T:System.NotSupportedException">Creation of <see cref="T:System.TypedReference"></see>, <see cref="T:System.ArgIterator"></see>, and <see cref="T:System.RuntimeArgumentHandle"></see> types is not supported.</exception>
		/// <exception cref="T:System.MethodAccessException">The constructor is private or protected, and the caller lacks <see cref="F:System.Security.Permissions.ReflectionPermissionFlag.MemberAccess"></see>. </exception>
		/// <exception cref="T:System.MemberAccessException">The class is abstract.-or- The constructor is a class initializer. </exception>
		/// <exception cref="T:System.ArgumentException">The parameters array does not contain values that match the types accepted by this constructor, under the constraints of the binder. </exception>
		/// <exception cref="T:System.Security.SecurityException">The caller does not have the necessary code access permissions.</exception>
		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return _type.CreateInstance<object>();
		}

		/// <summary>
		/// When overridden in a derived class, invokes the reflected method or constructor with the given parameters.
		/// </summary>
		/// <param name="obj">The object on which to invoke the method or constructor. If a method is static, this argument is ignored. If a constructor is static, this argument must be null or an instance of the class that defines the constructor.</param>
		/// <param name="invokeAttr">A bit mask that is a combination of 0 or more bit flags from <see cref="T:System.Reflection.BindingFlags"></see>. If binder is null, this parameter is assigned the value <see cref="F:System.Reflection.BindingFlags.Default"></see>; thus, whatever you pass in is ignored.</param>
		/// <param name="binder">An object that enables the binding, coercion of argument types, invocation of members, and retrieval of <see cref="MemberInfo"/> objects via reflection. If binder is null, the default binder is used.</param>
		/// <param name="parameters">An argument list for the invoked method or constructor. This is an array of objects with the same number, order, and type as the parameters of the method or constructor to be invoked. If there are no parameters, this should be null.If the method or constructor represented by this instance takes a ByRef parameter, there is no special attribute required for that parameter in order to invoke the method or constructor using this function. Any object in this array that is not explicitly initialized with a value will contain the default value for that object type. For reference-type elements, this value is null. For value-type elements, this value is 0, 0.0, or false, depending on the specific element type.</param>
		/// <param name="culture">An instance of <see cref="CultureInfo"/> used to govern the coercion of types. If this is null, the <see cref="CultureInfo"/> for the current thread is used. (This is necessary to convert a String that represents 1000 to a Double value, for example, since 1000 is represented differently by different cultures.)</param>
		/// <returns>
		/// An Object containing the return value of the invoked method, or null in the case of a constructor, or null if the method's return type is void. Before calling the method or constructor, Invoke checks to see if the user has access permission and verify that the parameters are valid.
		/// </returns>
		/// <exception cref="T:System.MethodAccessException">The caller does not have permission to execute the constructor. </exception>
		/// <exception cref="T:System.InvalidOperationException">The type that declares the method is an open generic type. That is, the <see cref="P:System.Type.ContainsGenericParameters"></see> property returns true for the declaring type.</exception>
		/// <exception cref="T:System.Reflection.TargetInvocationException">The invoked method or constructor throws an exception. </exception>
		/// <exception cref="T:System.Reflection.TargetParameterCountException">The parameters array does not have the correct number of arguments. </exception>
		/// <exception cref="T:System.ArgumentException">The type of the parameters parameter does not match the signature of the method or constructor reflected by this instance. </exception>
		/// <exception cref="T:System.Reflection.TargetException">The obj parameter is null and the method is not static.-or- The method is not declared or inherited by the class of obj. -or-A static constructor is invoked, and obj is neither null nor an instance of the class that declared the constructor.</exception>
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return _type.CreateInstance<object>();
		}

		/// <summary>
		/// When overridden in a derived class, returns the <see cref="T:System.Reflection.MethodImplAttributes"></see> flags.
		/// </summary>
		/// <returns>The <see cref="MethodImplAttributes"/> flags.</returns>
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return MethodImplAttributes.Runtime;
		}

		/// <summary>
		/// When overridden in a derived class, gets the parameters of the specified method or constructor.
		/// </summary>
		/// <returns>
		/// An array of type <see cref="ParameterInfo"/> containing information that matches the signature of the method (or constructor) reflected by this <see cref="MethodBase"/> instance.
		/// </returns>
		public override ParameterInfo[] GetParameters()
		{
			return Array.Empty<ParameterInfo>();
		}

		/// <summary>
		/// When overridden in a derived class, returns an array of custom attributes identified by <see cref="T:System.Type"></see>.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for. Only attributes that are assignable to this type are returned.</param>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain to find the attributes.</param>
		/// <returns>
		/// An array of custom attributes applied to this member, or an array with zero (0) elements if no attributes have been applied.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="attributeType"/> is null.</exception>
		/// <exception cref="T:System.TypeLoadException">The custom attribute type cannot be loaded. </exception>
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return Array.Empty<object>();
		}

		/// <summary>
		/// When overridden in a derived class, returns an array containing all the custom attributes.
		/// </summary>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain to find the attributes.</param>
		/// <returns>
		/// An array that contains all the custom attributes, or an array with zero elements if no attributes are defined.
		/// </returns>
		public override object[] GetCustomAttributes(bool inherit)
		{
			return Array.Empty<object>();
		}

		/// <summary>
		/// When overridden in a derived class, indicates whether one or more instance of <paramref name="attributeType"/> is applied to this member.
		/// </summary>
		/// <param name="attributeType">The Type object to which the custom attributes are applied.</param>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain to find the attributes.</param>
		/// <returns>
		/// true if one or more instance of <paramref name="attributeType"/> is applied to this member; otherwise false.
		/// </returns>
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return false;
		}

		#endregion
//#endif

		#region Object Members

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return typeof(void).Name + " " + Name + "()";
		}

		#endregion
	}
}