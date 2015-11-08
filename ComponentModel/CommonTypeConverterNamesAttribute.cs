namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	[AttributeUsage(ReflectionHelper.Types)]
	//[CLSCompliant(false)]
	public class CommonTypeConverterNamesAttribute : Attribute
	{
		#region CommonTypeConverterNamesAttribute.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="CommonTypeConverterNamesAttribute"/> class.
		/// </summary>
		/// <param name="members">The members.</param>
		public CommonTypeConverterNamesAttribute(string members)
		{
			if (members.IsEmpty())
				throw new ArgumentNullException(nameof(members));

			_members = members.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (_members.IsEmpty())
				throw new ArgumentException("members");
		}

		#endregion

		#region Members

		private readonly string[] _members;

		/// <summary>
		/// Gets the members.
		/// </summary>
		/// <value>The members.</value>
		public string[] Members => _members;

		#endregion
	}
}