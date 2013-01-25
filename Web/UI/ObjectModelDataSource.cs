namespace Ecng.Web.UI
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.WebControls;
	using System.Web.UI.Design.WebControls;
	using System.Drawing.Design;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Path;

	#endregion

	[PersistChildren(false)]
	[ParseChildren(true)]
	[DefaultProperty("RootType")]
	[ToolboxData("<{0}:ObjectModelDataSource runat=\"server\"></{0}:ObjectModelDataSource>")]
	public class ObjectModelDataSource : DataSourceControl
	{
		#region Private Fields

		private ObjectModelDataSourceView _view;

		#endregion

		#region Path

		private string _path;

		[DefaultValue("")]
		public string Path
		{
			get { return _path; }
			set
			{
				_path = value;
				GetView();
			}
		}

		#endregion

		#region RootType

		private string _rootType;

		[DefaultValue("")]
		public string RootType
		{
			get
			{
				return Root != null ? Root.GetType().To<string>() : _rootType;
			}
			set
			{
				_rootType = value;
				GetView();
			}
		}

		#endregion

		#region Root

		private object _root;

		public object Root
		{
			get { return _root; }
			set
			{
				_root = value;
				GetView();
			}
		}

		#endregion

		#region PathParameters

		[Editor(typeof(ParameterCollectionEditor), typeof(UITypeEditor))]
		[MergableProperty(false)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[DefaultValue(null)]
		public ParameterCollection PathParameters
		{
			get
			{
				return GetView().PathParameters;
			}
		}

		#endregion

		#region SortExression

		private string _sortExression;

		[DefaultValue("")]
		public string SortExression
		{
			get { return _sortExression; }
			set
			{
				_sortExression = value;
				GetView().SortExpression = value;
			}
		}

		#endregion

		#region SortDirection

		private SortDirection _sortDirection;

		[DefaultValue("")]
		public SortDirection SortDirection
		{
			get { return _sortDirection; }
			set
			{
				_sortDirection = value;
				GetView().SortDirection = value;
			}
		}

		#endregion

		#region DataSourceControl Members

		protected override DataSourceView GetView(string viewName)
		{
			if (!RootType.IsEmpty() && !Path.IsEmpty())
			{
				var proxy = RootType.To<Type>().GetMember<MemberProxy>(Path);

				if (_view == null || _view.Proxy != proxy)
					_view = CreateView(this, viewName, proxy, Root, base.Context);
			}

			return _view;
		}

		protected override ICollection GetViewNames()
		{
			return new[] { "DefaultView" };
		}

		#endregion

		protected virtual ObjectModelDataSourceView CreateView(ObjectModelDataSource owner, string viewName, MemberProxy proxy, object root, HttpContext context)
		{
			return new ObjectModelDataSourceView(owner, viewName, proxy, root, context);
		}

		#region GetView

		private ObjectModelDataSourceView GetView()
		{
			return (ObjectModelDataSourceView)GetView("DefaultView");
		}

		#endregion
	}
}