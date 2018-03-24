namespace Ecng.Web
{
	using System.Collections.Generic;
	using System.Text;
	using System.Web;
	using System.Collections.Specialized;
	using System.Net;
	using System.Security;
	using System.Web.Configuration;
	using System.Web.Routing;

	using Ecng.Collections;
	using Ecng.Configuration;
#if !SILVERLIGHT
	using System;
	using System.Collections;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Drawing.Drawing2D;
	using System.IO;
	using System.Reflection;
	using System.Threading;
	using System.Web.Compilation;
	using System.Web.Security;
	using System.Web.UI;
	using System.Web.UI.WebControls;
	using Image = System.Web.UI.WebControls.Image;

	using AjaxControlToolkit;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;
#endif

	public static class HttpHelper
	{
#if !SILVERLIGHT
		private static readonly FastInvoker<VoidType, string, string> _getMimeType;

		static HttpHelper()
		{
			var type = "System.Web.MimeMapping, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B03F5F7F11D50A3A".To<Type>();

			IDictionary dict;

			if (TypeHelper.IsNet45OrNewer())
			{
				dict = type.GetValue<VoidType, object>("_mappingDictionary", null).GetValue<object, VoidType, IDictionary>("_mappings", null);
			}
			else
			{
				// BUG in .NET 3.5 SP1 https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=297416&wa=wsignin1.0
				type.GetMember<FieldInfo>("_extensionToMimeMappingTable").GetValue(null);
				dict = type.GetValue<VoidType, IDictionary>("_extensionToMimeMappingTable", null);
			}

			_getMimeType = FastInvoker<VoidType, string, string>.Create(type.GetMember<MethodInfo>("GetMimeMapping"));

			// инициализация словаря
			GetMimeType("test.html");

			if (!dict.Contains(".png"))
			{
				dict.Add(".png", "image/png");
				//type.SetValue("AddMimeMapping", new object[] { ".png", "image/png" });
			}

			if (HttpContext.Current != null)
			{
				try
				{
					DefaultMapper = new SiteMapMapper();
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		public static SiteMapMapper DefaultMapper { get; private set; }

		#region CurrentHandler

		/// <summary>
		/// Gets the current handler.
		/// </summary>
		/// <value>The current handler.</value>
		public static IHttpHandler CurrentHandler => HttpContext.Current.Handler;

		#endregion

		#region CurrentPage

		/// <summary>
		/// Gets the current handler.
		/// </summary>
		/// <value>The current handler.</value>
		public static Page CurrentPage => HttpContext.Current.Handler as Page;

		#endregion

		public static TItem GetDataItem<TItem>(this Page page)
		{
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			return (TItem)page.GetDataItem();
		}

		public static string GetMimeType(this string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			if (fileName[0] != '.')
				fileName = '.' + fileName;

			return _getMimeType.ReturnInvoke('\\' + fileName);
		}

		public static string GetMimeType(this IWebFile file)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			return file.Name.GetMimeType();
		}

		public const int DefaultPageSize = 20;

		public static Type ToPageType(this Uri url)
		{
			return url.ToString().ToPageType();
		}

		public static Type ToPageType(this SiteMapNode node)
		{
			return node.Url.ToPageType();
		}

		public static Type ToPageType(this string virtualPath)
		{
			var pageType = BuildManager.GetCompiledType(virtualPath);

			if (!virtualPath.Contains("ashx"))
				pageType = pageType.BaseType;

			return pageType;
		}

		public static void Redirect(this Type pageType)
		{
			new Url(pageType).Redirect();
		}

		public static void Redirect(this Url url, bool endResponse = true)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			var localPath = url.LocalPath;

			if (localPath.EndsWithIgnoreCase("Default.aspx") && !url.KeepDefaultPage)
				localPath = localPath.ReplaceIgnoreCase("Default.aspx", string.Empty);

			HttpContext.Current.Response.Redirect(new Uri(url.Clone(), localPath).ToString() + url.QueryString, endResponse);
		}

		public static void RegisterScript<T>(this T control, string key, string script)
			where T : Control
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			if (!control.Page.ClientScript.IsStartupScriptRegistered(typeof(T), key))
				control.Page.ClientScript.RegisterStartupScript(typeof(T), key, script, true);
		}

		public static void AuthorizedAction(Action action)
		{
			if (!Thread.CurrentPrincipal.Identity.IsAuthenticated)
				FormsAuthentication.RedirectToLoginPage();
			else
				action();
		}

		public static void SetChildValue<T>(this Control parent, string id, T value)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			var control = parent.FindControl(id);

			if (control == null)
				throw new ArgumentException("Control not founded with id '{0}'".Put(id), nameof(id));

			control.SetValue(value);
		}

		public static void SetValue<T>(this Control control, T value)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			if (control is TextBox)
				((TextBox)control).Text = value.To<string>();
			else if (control is ListControl)
				((ListControl)control).SelectedValue = value.To<string>();
			else if (control is Calendar)
				((Calendar)control).SelectedDate = value.To<DateTime>();
			else if (control is CheckBox)
				((CheckBox)control).Checked = value.To<bool>();
			else if (control is Label)
				((Label)control).Text = value.To<string>();
			else if (control is Rating)
				((Rating)control).CurrentRating = value.To<int>();
			else if (control is Image)
				((Image)control).ImageUrl = value.To<string>();
			else
				throw new ArgumentException("Control with id '{0}' has unsupported type '{1}'.".Put(control.ID, control.GetType()), nameof(control));
		}

		public static T GetChildValue<T>(this Control parent, string id)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			var control = parent.FindControl(id);

			if (control == null)
				throw new ArgumentException("Control not founded with id '{0}'".Put(id), nameof(id));

			return control.GetValue<T>();
		}

		public static T GetValue<T>(this Control control)
		{
			object value;

			if (control is TextBox)
				value = ((TextBox)control).Text;
			else if (control is ListControl)
				value = ((ListControl)control).SelectedValue;
			else if (control is Calendar)
				value = ((Calendar)control).SelectedDate;
			else if (control is CheckBox)
				value = ((CheckBox)control).Checked;
			else if (control is Label)
				value = ((Label)control).Text;
			else if (control is Rating)
				value = ((Rating)control).CurrentRating;
			else if (control is Image)
				value = ((Image)control).ImageUrl;
			else
				throw new ArgumentException("Control with id '{0}' has unsupported type '{1}'.".Put(control.ID, control.GetType()), nameof(control));

			return value.To<T>();
		}

		public static void Download(this IWebFile file, HttpContext context = null)
		{
			file.Download(new Size<int>(), false, context);
		}

		public static void Download(this IWebFile file, Size<int> size, bool embed, HttpContext context = null, TimeSpan? cache = null)
		{
			if (context == null)
				context = HttpContext.Current;

			if (file == null)
				throw new ArgumentNullException(nameof(file));

			// http://stackoverflow.com/questions/994135/image-from-httphandler-wont-cache-in-browser
			var body = file.ShrinkFile(size);
			var response = context.Response;

			response.ContentType = file.GetMimeType();

			if (cache != null)
			{
				response.Cache.SetCacheability(HttpCacheability.Public);
				response.Cache.SetExpires(DateTime.Now + cache.Value);
				response.Cache.SetMaxAge(cache.Value);
			}

			response.AppendHeader("Content-Disposition", "{0}; filename=\"{1}\"".Put(embed ? "inline" : "attachment", file.Name));
			response.OutputStream.Write(body, 0, body.Length);
			response.End();
		}

		public static byte[] ShrinkFile(this IWebFile file, Size<int> size)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			return ShrinkFile(file.Body, size);
		}

		public static byte[] ShrinkFile(this byte[] file, Size<int> size)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			if (size == null)
				throw new ArgumentNullException(nameof(size));

			var body = file.To<Stream>();

			if (body == null)
				throw new ArgumentException("file");

			if (size.Width > 0 && size.Height > 0)
			{
				using (var srcImage = new Bitmap(body))
				{
					var coeff = (double)srcImage.Width / size.Width;

					if (coeff <= 1)
						coeff = (double)srcImage.Height / size.Height;

					if (coeff > 1)
					{
						var newWidth = (int)(srcImage.Width / coeff);
						var newHeight = (int)(srcImage.Height / coeff);

						body = new MemoryStream();
#if SILVERLIGHT
						using (var newImage = srcImage.GetThumbnailImage(newWidth, newHeight, () => false, IntPtr.Zero))
						{
#else
						using (var newImage = new Bitmap(newWidth, newHeight))
						{
							using (var gr = Graphics.FromImage(newImage))
							{
								gr.SmoothingMode = SmoothingMode.HighQuality;
								gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
								gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
								gr.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
							}
#endif
							newImage.Save(body, ImageFormat.Png);
						}

					}
				}
			}

			return body.To<byte[]>();
		}

		public static string EncodeToHtml(this string text)
		{
			return HttpUtility.HtmlEncode(text);
		}
#endif

		private static readonly Encoding _urlEncoding = Encoding.UTF8;

		public static string EncodeUrl(this string url)
		{
			return HttpUtility.UrlEncode(url, _urlEncoding);
		}

		public static string DecodeUrl(this string url)
		{
			return HttpUtility.UrlDecode(url, _urlEncoding);
		}

		public static string UrlEncodeToUpperCase(this string url)
		{
			if (url == null)
				return null;

			var temp = url.ToCharArray();

			for (var i = 0; i < temp.Length - 2; i++)
			{
				if (temp[i] != '%')
					continue;

				temp[i + 1] = char.ToUpper(temp[i + 1]);
				temp[i + 2] = char.ToUpper(temp[i + 2]);
			}

			return new string(temp);
		}

		public static NameValueCollection ParseUrl(this string url)
		{
			return HttpUtility.ParseQueryString(url, _urlEncoding);
		}

		public static IWebUser TryGetByNameOrEmail(this IWebUserCollection users, string id)
		{
			if (users == null)
				throw new ArgumentNullException(nameof(users));

			return users.GetByName(id) ?? users.GetByEmail(id);
		}

		public static RequestContext GetCurrentRouteRequest()
		{
			return HttpContext.Current.Request.RequestContext;
		}

		public static string XmlEscape(this string content)
		{
			return SecurityElement.Escape(content);
		}

		public static string ClearUrl(this string url)
		{
			var chars = new List<char>(url);

			var count = chars.Count;

			for (var i = 0; i < count; i++)
			{
				if (!IsUrlSafeChar(chars[i]))
				{
					chars.RemoveAt(i);
					count--;
					i--;
				}
			}

			return new string(chars.ToArray());
		}

		public static bool IsUrlSafeChar(this char ch)
		{
			if (((ch < 'a') || (ch > 'z')) && ((ch < 'A') || (ch > 'Z')) && ((ch < '0') || (ch > '9')))
			{
				switch (ch)
				{
					case '(':
					case ')':
					//case '*':
					case '-':
					//case '.':
					case '!':
						break;

					case '+':
					case ',':
					case '.':
					case '%':
					case '*':
						return false;

					default:
						if (ch != '_')
							return false;

						break;
				}
			}

			return true;
		}

		public static void Make403(this HttpResponse response)
		{
			if (response == null)
				throw new ArgumentNullException(nameof(response));

			response.TrySkipIisCustomErrors = true;
			response.StatusDescription = "403 Forbidden";
			response.StatusCode = (int)HttpStatusCode.Forbidden;
		}

		public static void Make404(this HttpResponse response)
		{
			if (response == null)
				throw new ArgumentNullException(nameof(response));

			response.TrySkipIisCustomErrors = true;
			response.StatusDescription = "404 Not Found";
			response.StatusCode = (int)HttpStatusCode.NotFound;
		}

		private static readonly SynchronizedSet<string> _imgExts = new SynchronizedSet<string>
		{
			".png", ".jpg", ".jpeg", ".bmp", ".gif"
		};

		public static bool IsImage(this IWebFile file)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			var ext = Path.GetExtension(file.Name);

			if (ext.IsEmpty())
				return false;

			return _imgExts.Contains(ext.ToLowerInvariant());
		}

		// http://stackoverflow.com/a/1306932
		public static void ForceSignOff(this HttpContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			FormsAuthentication.SignOut();
			context.Session.Abandon();

			// clear authentication cookie
			context.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, string.Empty)
			{
				Expires = DateTime.Now.AddYears(-1)
			});

			// clear session cookie (not necessary for your current problem but i would recommend you do it anyway)
			var sessionStateSection = ConfigManager.GetSection<SessionStateSection>();
			context.Response.Cookies.Add(new HttpCookie(sessionStateSection.CookieName, string.Empty)
			{
				Expires = DateTime.Now.AddYears(-1)
			});

			FormsAuthentication.RedirectToLoginPage();
		}
	}
}