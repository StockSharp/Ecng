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
			Member = member ?? throw new ArgumentNullException(nameof(member));

			ReturnType = member is ConstructorInfo ? typeof(void) : member.GetMemberType();

			if (member is MethodBase mb)
				ParamTypes = [.. mb.GetParameterTypes().Select(t => t.type)];
			else if (member.IsIndexer())
				ParamTypes = [.. ((PropertyInfo)member).GetIndexerTypes()];
			else
				ParamTypes = [];
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

		protected override bool OnEquals(MemberSignature other)
		{
			if (ReturnType != other.ReturnType)
				return false;

			return ParamTypes.SequenceEqual(other.ParamTypes);
		}

		public override int GetHashCode()
			=> ReturnType.GetHashCode() ^ ParamTypes.GetHashCodeEx();

		public override MemberSignature Clone()	=> new(Member);

		public override string ToString() => Member.ToString();
	}
}