namespace Ecng.Forum.Components
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Web;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Net;
	using Ecng.Web;
	using Ecng.Forum.BusinessEntities;

	public class ForumWebApplication : WebApplication
	{
		private readonly TimeSpan _updateUserInterval = new TimeSpan(TimeSpan.TicksPerMinute);

		private readonly List<Visit> _visits = new List<Visit>();
		private readonly object _visitLock = new object();
		private DateTime _visitUpdateLastTime = DateTime.Now;
		private readonly TimeSpan _visitUpdateInterval = new TimeSpan(TimeSpan.TicksPerMinute);

		protected override void OnAppEnd(EventArgs e)
		{
			ForumHelper.Logger.Log(HttpRuntimeEx.Current.Error);
			base.OnAppEnd(e);
		}

		protected override void OnAppError(ErrorEventArgs e)
		{
			ForumHelper.Logger.Log(new HttpException("Access to url '{0}' thrown exception.".Put(Url.Current), e.GetException()));
			base.OnAppError(e);
		}

		protected override void OnAppEndRequest(EventArgs e)
		{
			var rootObject = ForumHelper.GetRootObject<ForumRootObject>();

			if (HttpContext.Current.Handler != null)
			{
				var pageId = VisitPageAttribute.GetId(HttpContext.Current.Handler);
				if (pageId != null)
				{
					lock (_visitLock)
					{
						var urlReferrer = Request.UrlReferrer;
						
						byte? prevPageId = null;

						if (urlReferrer != null && urlReferrer.ToString().ToLowerInvariant().Contains(AspNetPath.PortalName.ToLowerInvariant()))
						{
							ForumHelper.Logger.AutoLog(() =>
							{
								try
								{
									var localPath = urlReferrer.LocalPath;
									if (!localPath.IsEmpty())
									{
										if (localPath[localPath.Length - 1] == '/')
											localPath += AspNetPath.DefaultPage;

										prevPageId = VisitPageAttribute.GetId(localPath.ToPageType());
									}
								}
								catch (Exception ex)
								{
									throw new HttpException("Url '{0}' with local path '{1}' can't be parsed.".Put(urlReferrer, urlReferrer.LocalPath), ex);
								}
							});
						}

						_visits.Add(new Visit
						{
							PageId = pageId.Value,
							IpAddress = NetworkHelper.UserAddress,
							User = ForumHelper.CurrentUser ?? rootObject.Users.Null,
							QueryString = Url.Current.QueryString.ToString(),
							PrevPageId = prevPageId,
							UrlReferrer = prevPageId == null && urlReferrer != null ? urlReferrer.ToString() : null,
							UserAgent = Request.UserAgent,
						});
					}
				}
			}
			//else
			//	System.Diagnostics.Debug.WriteLine(HttpContext.Current.Request.Url);

			if ((DateTime.Now - _visitUpdateLastTime) > _visitUpdateInterval)
			{
				lock (_visitLock)
				{
					rootObject.Visits.AddRange(_visits);
					_visits.Clear();
					_visitUpdateLastTime = DateTime.Now;
				}
			}

			var user = ForumHelper.CurrentUser;

			if (user != null)
			{
				user.LastActivityDate = DateTime.Now;

				if ((DateTime.Now - user.LastActivityDate) > _updateUserInterval)
					rootObject.Users.Update(user);
			}

			base.OnAppBeginRequest(e);
		}
	}
}