namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Common;

	#endregion

	public class InterceptContext
	{
		#region Private Fields

		private readonly InterceptTypes _types;

		#endregion

		#region InterceptContext.ctor()

		internal InterceptContext(object instance, InterceptTypes types, MethodBase method, IDictionary<string, object> inRefArgs)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (method == null)
				throw new ArgumentNullException("method");

			if (inRefArgs == null)
				throw new ArgumentNullException("inRefArgs");

			Instance = instance;
			_types = types;
			Method = method;
			InRefArgs = inRefArgs;
		}

		#endregion

		public object Instance { get; private set; }
		public MethodBase Method { get; private set; }
		public IDictionary<string, object> InRefArgs { get; private set; }

		#region RefOutArgs

		private IDictionary<string, object> _refOutArgs;

		public IDictionary<string, object> RefOutArgs
		{
			get { return _refOutArgs; }
			internal set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_refOutArgs = value;
			}
		}

		#endregion

		#region ReflectedType

		public Type ReflectedType
		{
			get { return Method.ReflectedType; }
		}

		#endregion

		#region MethodName

		public string MethodName
		{
			get { return Method.Name; }
		}

		#endregion

		#region HasReturnValue

		public bool HasReturnValue
		{
			get { return Method.GetMemberType() != typeof(void); }
		}

		#endregion
	
		public object ReturnValue { get; internal set; }
		public Exception Exception { get; internal set; }

		internal bool CanProcess(InterceptTypes type)
		{
			return _types.Contains(type);
		}
	}
}