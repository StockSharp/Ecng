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
			FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
		}

		#endregion

		#region FactoryType

		/// <summary>
		/// Gets the type of the factory.
		/// </summary>
		/// <value>The type of the factory.</value>
		public Type FactoryType { get; }

		#endregion
	}
}