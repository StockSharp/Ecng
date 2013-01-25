namespace Ecng.Data.Sql
{
	#region Using Directives

	using System;
	using System.Data;

	using Ecng.Common;
	using Ecng.ComponentModel;

	#endregion

	public struct ProcParam
	{
		#region ProcParam.ctor()

		public ProcParam(string name, DbType type, Range<int> length)
		{
			_name = null;
			_type = DbType.Object;
			_length = null;

			Name = name;
			Type = type;
			Length = length;
		}

		#endregion

		#region Name

		private string _name;

		public string Name
		{
			get { return _name; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_name = value;
			}
		}

		#endregion

		#region Type

		private DbType _type;

		public DbType Type
		{
			get { return _type; }
			set { _type = value; }
		}

		#endregion

		#region Size

		private Range<int> _length;

		public Range<int> Length
		{
			get { return _length; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_length = value;
			}
		}

		#endregion
	}
}