namespace Ecng.Reflection.Emit
{
	using System.Reflection.Emit;

	public class GenericArgGenerator : BaseGenerator<GenericTypeParameterBuilder>
	{
		internal GenericArgGenerator(GenericTypeParameterBuilder builder)
			: base(builder)
		{
			Constraints = new ConstraintList(builder);
		}

		public ConstraintList Constraints { get; }
	}
}