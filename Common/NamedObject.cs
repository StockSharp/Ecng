namespace Ecng.Common
{
	public abstract class NamedObject
	{
		protected NamedObject(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}