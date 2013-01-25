namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System.Reflection.Emit;

	#endregion

	public class LocalGenerator : BaseGenerator<LocalBuilder>
	{
		//#region Private Fields

		//private LocalBuilder _local;

		//#endregion

		#region LocalGenerator.ctor()

		internal LocalGenerator(LocalBuilder local)
			: base(local)
		{
			//_local = local;
		}

		#endregion

		//#region Type

		//public Type Type
		//{
		//    get { return _local.LocalType; }
		//}

		//#endregion

		//#region IsPinned

		//public bool IsPinned
		//{
		//    get { return _local.IsPinned; }
		//}

		//#endregion

		//#region Index

		//public byte Index
		//{
		//    get { return (byte)_local.LocalIndex; }
		//}

		//#endregion
	}
}