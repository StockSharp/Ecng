namespace Ecng.Data
{
	#region Using Directives

	#endregion

	public sealed class MappingContext
	{
		internal MappingContext(DatabaseCommand command, object entity)
		{
			this.Command = command;
			this.Entity = entity;
		}

		public DatabaseCommand Command { get; private set; }
		public object Entity { get; private set; }
	}
}