namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System.Reflection.Emit;

	#endregion

	public class ParameterGenerator : BaseGenerator<ParameterBuilder>
	{
		#region ParameterGenerator.ctor()

		internal ParameterGenerator(ParameterBuilder builder)
			: base(builder)
		{
		}

		#endregion
	}
}