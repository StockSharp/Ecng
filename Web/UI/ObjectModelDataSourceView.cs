namespace Ecng.Web.UI
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.WebControls;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection.Path;
	using Ecng.Collections;
	using Ecng.Reflection;

	#endregion

	public class ObjectModelDataSourceView : DataSourceView, IStateManager
	{
		#region ObjectModelDataSourceView.ctor()

		public ObjectModelDataSourceView(ObjectModelDataSource owner, string name, MemberProxy proxy, object root, HttpContext context)
			: base(owner, name)
		{
			if (proxy == null)
				throw new ArgumentNullException(nameof(proxy));

			//if (context == null)
			//	throw new ArgumentNullException(nameof(context));

			LastItemType = proxy.Items.Last().Invoker.Member.GetMemberType();

			var colTypes = new[] { typeof(IList<>), typeof(ICollection<>), typeof(IEnumerable<>) };

			foreach (var colType in colTypes)
			{
				if (LastItemType.GetGenericType(colType) != null)
				{
					ItemType = LastItemType.GetGenericTypeArg(colType, 0);
					break;
				}
			}

			if (ItemType == null)
				ItemType = LastItemType;

            Proxy = proxy;
			Root = root;
            Owner = owner;
            Context = context;
		}

		#endregion

		public Type LastItemType { get; }
		public Type ItemType { get; }
		public MemberProxy Proxy { get; }
		public object Root { get; }
		protected ObjectModelDataSource Owner { get; }
		protected HttpContext Context { get; }

		#region Path

		public string Path
		{
			get
			{
				var path = ViewState["Path"];

				if (path == null)
					return string.Empty;

				return (string)path;
			}
			set
			{
				if (Path != value)
				{
					ViewState["Path"] = value;
					base.OnDataSourceViewChanged(EventArgs.Empty);
				}
			}
		}

		#endregion

		#region PathParameters

		private ParameterCollection _pathParameters;

		public ParameterCollection PathParameters
		{
			get
			{
				if (_pathParameters == null)
				{
					_pathParameters = new ParameterCollection();
					_pathParameters.ParametersChanged += (sender, e) => OnDataSourceViewChanged(e);

					if (_isTrackingViewState)
						((IStateManager)_pathParameters).TrackViewState();
				}

				return _pathParameters;
			}
		}

		#endregion

		#region ViewState

		private StateBag _viewState;

		protected StateBag ViewState
		{
			get
			{
				if (_viewState == null)
				{
					_viewState = new StateBag();

					if (_isTrackingViewState)
						((IStateManager)_viewState).TrackViewState();
				}

				return _viewState;
			}
		}

		#endregion

		#region MaximumRows

		public int MaximumRows
		{
			get
			{
				var rows = ViewState["MaximumRows"];

				if (rows == null)
					return WebHelper.DefaultPageSize;

				return (int)rows;
			}
			set
			{
				if (MaximumRows != value)
				{
					ViewState["MaximumRows"] = value;
					base.OnDataSourceViewChanged(EventArgs.Empty);
				}
			}
		}

		#endregion

		public string SortExpression { get; set; }

		public ListSortDirection SortDirection { get; set; }

		#region DataSourceView Members

		public override bool CanUpdate => true;

		public override bool CanDelete => true;

		public override bool CanInsert => true;

		public override bool CanSort => true;

		public override bool CanPage => true;

		public override bool CanRetrieveTotalRowCount => true;

		protected override IEnumerable ExecuteSelect(DataSourceSelectArguments arguments)
		{
			if (LastItemType != ItemType)
			{
				var collection = GetCollection();

				if (collection == null)
				{
					arguments.TotalRowCount = 0;
					return Enumerable.Empty<object>();
				}

				if (arguments.RetrieveTotalRowCount)
					arguments.TotalRowCount = GetCount(collection);

				if (arguments.MaximumRows == 0 || arguments.MaximumRows == -1)
					arguments.MaximumRows = MaximumRows;

				string orderBy;
				ListSortDirection direction;

				var sortExpression = arguments.SortExpression;

				if (sortExpression.IsEmpty())
					sortExpression = SortExpression;

				if (sortExpression.IsEmpty())
				{
					orderBy = null;
					direction = SortDirection;
				}
				else
				{
					if (sortExpression.Contains(" "))
					{
						var parts = sortExpression.Split(' ');
						orderBy = parts[0];

						direction = (parts[1] + "ending").To<ListSortDirection>();
					}
					else
					{
						orderBy = sortExpression;
						direction = SortDirection;
					}
				}

				return GetRange(collection, arguments.StartRowIndex, arguments.MaximumRows, orderBy, direction);
			}
			else
				return new[] { GetItem() };
		}

		protected override int ExecuteDelete(IDictionary keys, IDictionary oldValues)
		{
			//return base.ExecuteDelete(keys, oldValues);
			return keys.Count;
		}

		#endregion

		protected virtual int GetCount(IRangeCollection collection)
		{
			return collection.Count;
		}

		protected virtual IEnumerable GetRange(IRangeCollection collection, int startIndex, int count, string sortExpression, ListSortDirection direction)
		{
			return collection.GetRange(startIndex, count, sortExpression, direction);
		}

		protected virtual object GetItem()
		{
			return Invoke();
		}

		#region CreateArgs

		private IDictionary<string, object> CreateArgs()
		{
			return PathParameters.GetValues(Context, Owner).TypedAs<string, object>();
		}

		#endregion

		#region GetList

		protected virtual IRangeCollection GetCollection()
		{
			return (IRangeCollection)Invoke();
		}

		#endregion

		private object Invoke()
		{
			return Proxy.Invoke(Root, CreateArgs());
		}

		#region IStateManager Members

		private bool _isTrackingViewState;

		bool IStateManager.IsTrackingViewState => _isTrackingViewState;

		void IStateManager.LoadViewState(object state)
		{
			if (state != null)
			{
				var viewState = (object[])state;

				if (viewState[0] != null)
					((IStateManager)ViewState).LoadViewState(viewState[0]);

				if (viewState[1] != null)
					((IStateManager)PathParameters).LoadViewState(viewState[1]);
			}
		}

		object IStateManager.SaveViewState()
		{
			var viewState = new[] { (_viewState != null) ? ((IStateManager)_viewState).SaveViewState() : null, (_pathParameters != null) ? ((IStateManager)_pathParameters).SaveViewState() : null };

			if (viewState[0] == null && viewState[1] == null)
				return null;
			else
				return viewState;
		}

		void IStateManager.TrackViewState()
		{
			_isTrackingViewState = true;

			if (_viewState != null)
				((IStateManager)_viewState).TrackViewState();

			if (_pathParameters != null)
				((IStateManager)_pathParameters).TrackViewState();
		}

		#endregion
	}
}