namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System.Reflection.Emit;

	#endregion

	public class FieldGenerator : BaseGenerator<FieldBuilder>
	{
		#region FieldGenerator.ctor()

		internal FieldGenerator(FieldBuilder field)
			: base(field)
		{
		}

		#endregion
	}
}