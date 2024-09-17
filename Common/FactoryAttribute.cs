namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

	/// <summary>
	/// Initializes a new instance of the <see cref="FactoryAttribute"/> class.
	/// </summary>
	/// <param name="factoryType">Type of the factory.</param>
	public abstract class FactoryAttribute(Type factoryType) : Attribute
	{

		#region FactoryAttribute.ctor()

		#endregion

		#region FactoryType

		/// <summary>
		/// Gets the type of the factory.
		/// </summary>
		/// <value>The type of the factory.</value>
		public Type FactoryType { get; } = factoryType ?? throw new ArgumentNullException(nameof(factoryType));

		#endregion
	}
}