namespace Ecng.Forum.Components
{
	using Ecng.Common;
	using Ecng.Web;
	using Ecng.Web.UI.WebControls;
	using Ecng.Forum.BusinessEntities;
	using Ecng.Logic.BusinessEntities;

	public abstract class BaseEntityLink<T> : HyperLinkEx
		where T : BaseEntity<ForumUser, ForumRole>
	{
		public int? Trim { get; set; }

		#region Entity

		private T _entity;

		public T Entity
		{
			get { return _entity; }
			set
			{
				if (value != null)
				{
					var namedEntity = GetNamedEntity(value);

					if (Controls.Count == 0)
					{
						var text = namedEntity.Name;

						if (Trim != null)
							text = text.Trim((int)Trim);

						Text = text;
					}

					ToolTip = namedEntity.Description;
					NavigateUrl = GetUrl(value).ToString();
				}
				else
				{
					if (Controls.Count == 0)
						Text = string.Empty;

					ToolTip = string.Empty;
				}

				_entity = value;
			}
		}

		#endregion

		protected virtual ForumBaseNamedEntity GetNamedEntity(T entity)
		{
			return entity.To<ForumBaseNamedEntity>();
		}

		protected virtual Url GetUrl(T entity)
		{
			return ForumHelper.GetIdentityUrl(entity);
		}
	}
}