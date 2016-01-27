namespace Ecng.Forum.Components
{
	using System;
	using System.Diagnostics;
	using System.Security;

	using Ecng.Common;
	using Ecng.Forum.BusinessEntities;
	using Ecng.Net;
	using Ecng.Web;
	using Ecng.Transactions;

	public class ClientException : Exception
	{
		public ClientException(Exception innerException)
			: base(null, innerException)
		{
		}
	}

	public class Logger
	{
		public void AutoLog(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			try
			{
				action();
			}
			catch (Exception ex)
			{
				Log(ex);
			}
		}

		public void Log(Exception ex)
		{
			try
			{
				if (ex == null)
					throw new ArgumentNullException(nameof(ex));

				Forum forum;
				var forums = ForumHelper.GetRootObject<ForumRootObject>().Forums;

				if (ex is UnauthorizedAccessException || ex is SecurityException)
					forum = forums.SecurityErrors;
				else if (ex is HttpRuntimeShutdownException)
					forum = forums.ShutdownErrors;
				else if (ex is ClientException)
				{
					forum = forums.ClientErrors;
					ex = ex.InnerException;
				}
				else
					forum = forums.ServerErrors;

				Debug.WriteLine(ex);

				AutoComplete.Do(delegate
				{
					var topic = new Topic
					{
						Name = ex.GetType().Name,
					};
					forum.Topics.Add(topic);

					var message = new Message
					{
						Body = "{{{{{{{{{1}<nowiki>{0}</nowiki>{1}}}}}}}}}".Put(ex, Environment.NewLine),
						IpAddress = NetworkHelper.UserAddress
					};
					topic.Messages.Add(message);
				});
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}
	}
}