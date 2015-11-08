namespace Ecng.Forum.Components
{
	using System;

	using Ecng.Forum.BusinessEntities;
	using Ecng.Web.UI.WebControls;

	public class MessageWikiText : WikiText
	{
		private Message _message;

		public Message Message
		{
			get { return _message; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				Text = value.Body;

				_message = value;
			}
		}
	}
}