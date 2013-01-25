namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

	public abstract class FactoryAttribute : Attribute
	{
		#region FactoryAttribute.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="FactoryAttribute"/> class.
		/// </summary>
		/// <param name="factoryType">Type of the factory.</param>
		protected FactoryAttribute(Type factoryType)
		{
			if (factoryType == null)
				throw new ArgumentNullException("factoryType");

			_factoryType = factoryType;
		}

		#endregion

		#region FactoryType

		private readonly Type _factoryType;

		/// <summary>
		/// Gets the type of the factory.
		/// </summary>
		/// <value>The type of the factory.</value>
		public Type FactoryType
		{
			get { return _factoryType; }
		}

		#endregion
	}
}