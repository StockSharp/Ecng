namespace Ecng.Reflection.Path
{
	using System.Reflection;

	public enum ParamType
	{
		None,
		Direct,
		Reference,
	}

	public class Param
	{
		public string Name { get; set; }
		public ParameterInfo Info { get; set; }
		public ParamType Type { get; set; }
		public object Value { get; set; }
	}
}