namespace Ecng.Forum.Components
{
	#region Using Directives

	using System.Web.UI.WebControls;

	using Ecng.Common;
	using Ecng.Forum.BusinessEntities;
	using Ecng.Web;

	#endregion

	public abstract class FeedLink : HyperLink
	{
		private readonly Url _baseUrl;

		protected FeedLink(Url baseUrl)
		{
			_baseUrl = baseUrl;
		}

		private bool _isPrivateMessages;

		public bool IsPrivateMessages
		{
			get { return _isPrivateMessages; }
			set
			{
				_isPrivateMessages = value;
				var baseUrl = _baseUrl.Clone();
				baseUrl.QueryString.Append("pm", value);
				NavigateUrl = baseUrl.ToString();
				ToolTip = "Subscribe on private messages";
				Text = "pm";
			}
		}

		private ForumBaseEntity _entity;

		public ForumBaseEntity Entity
		{
			get { return _entity; }
			set
			{
				if (_entity != value)
				{
					_entity = value;

					if (value != null)
					{
						var baseUrl = _baseUrl.Clone();
						baseUrl.QueryString.Append(ForumHelper.GetIdentity(value.GetType()), value.Id);
						NavigateUrl = baseUrl.ToString();
						ToolTip = "Subscribe on {0} message list".Put(value.GetType().Name.ToLower());
						Text = value.GetType().Name.ToLower().Replace("link", string.Empty);
					}
					else
					{
						NavigateUrl = string.Empty;
						ToolTip = string.Empty;
						Text = string.Empty;
					}
				}
			}
		}
	}
}