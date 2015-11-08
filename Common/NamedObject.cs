namespace Ecng.Common
{
	#region Using Directives

	

	#endregion

	public abstract class NamedObject
	{
		#region NamedObject.ctor()

		protected NamedObject(string name)
		{
			_name = name;
		}

		#endregion

		#region Name

		private readonly string _name;

		public string Name => _name;

		#endregion
	}
}