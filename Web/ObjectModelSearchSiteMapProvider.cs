namespace Ecng.Web
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.ComponentModel;
    using System.Web.UI.WebControls;

	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Reflection;
	using Ecng.Reflection.Path;
	using Ecng.Serialization;

	using Microsoft.Web.Preview.Search;

	#endregion

	public class ObjectModelSearchSiteMapProvider : DynamicDataSearchSiteMapProvider
	{
		#region Private Fields

		[Ignore]
		private IListEx _list;

		#endregion

		public const int DefaultMaxCountValue = 1000;
		public const string DefaultSortExpressionValue = "ModificationDate";

		#region RootType

		private Type _rootType;

		public Type RootType
		{
			get { return _rootType; }
			set { _rootType = value; }
		}

		#endregion

		#region Path

		private string _path;

		public string Path
		{
			get { return _path; }
			set { _path = value; }
		}

		#endregion

		#region MaxCount

		[DefaultValue(DefaultMaxCountValue)]
		private int _maxCount = DefaultMaxCountValue;

		public int MaxCount
		{
			get { return _maxCount; }
			set { _maxCount = value; }
		}

		#endregion

		#region SortExpression

		[DefaultValue(DefaultSortExpressionValue)]
		[Field("lastModifiedDataField")]
		private string _sortExpression = DefaultSortExpressionValue;
		
		public string SortExpression
		{
			get { return _sortExpression; }
			set { _sortExpression = value; }
		}

		#endregion

		#region ProviderBase Members

		public override void Initialize(string name, NameValueCollection config)
		{
			ProviderInitializer.Initialize(this, config, true);
			base.Initialize(name, config);
		}

		#endregion

		#region SearchSiteMapProviderBase Members

		public override IEnumerable DataQuery()
		{
			if (_list == null)
			{
				var proxy = _rootType.GetMember<MemberProxy>(_path);
				_list = (IListEx)proxy.Invoke(null);
			}

			return _list.GetRange(0, _maxCount, _sortExpression, SortDirection.Descending);
		}

		#endregion
	}
}