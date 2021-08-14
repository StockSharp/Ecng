namespace Ecng.Reflection
{
	#region Using Directives

	using System;
	using System.Reflection;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	#endregion

	[Serializable]
	public class MemberSignature : Equatable<MemberSignature>
	{
		#region MemberSignature.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Ecng.Reflection.MemberSignature"/> class.
		/// </summary>
		/// <param name="member">The member.</param>
		public MemberSignature(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			Member = member;

			ReturnType = member is ConstructorInfo ? typeof(void) : member.GetMemberType();

			if (member is MethodBase mb)
				ParamTypes = mb.GetParameterTypes().Select(t => t.type).ToArray();
			else if (member.IsIndexer())
				ParamTypes = new [] { ((PropertyInfo)member).GetIndexerType() };
			else
				ParamTypes = ArrayHelper.Empty<Type>();
		}

		#endregion

		public MemberInfo Member { get; }

		/// <summary>
		/// Gets or sets the type of the return.
		/// </summary>
		/// <value>The type of the return.</value>
		public Type ReturnType { get; }

		/// <summary>
		/// Gets or sets the param types.
		/// </summary>
		/// <value>The param types.</value>
		public Type[] ParamTypes { get; }

		#region Equatable<MemberSignature> Members

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(MemberSignature other)
		{
			if (ReturnType != other.ReturnType)
				return false;

			return ParamTypes.SequenceEqual(other.ParamTypes);
		}

		#endregion

		#region Object Members

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return ReturnType.GetHashCode() ^ ParamTypes.GetHashCodeEx();
		}

		#endregion

		public override MemberSignature Clone()
		{
			return new MemberSignature(Member);
		}
	}
}