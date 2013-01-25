namespace Ecng.Web
{
	using System.Text;
	using System.Web;
	using System.Collections.Specialized;

#if !SILVERLIGHT
	using System;
	using System.Collections;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using System.Net;
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

		#region UserAddress

		/// <summary>
		/// Gets the user address.
		/// </summary>
		/// <value>The user address.</value>
		public static IPAddress UserAddress
		{
			get
			{
				return HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress.To<IPAddress>() : (GetUserAddress != null ? (GetUserAddress() ?? IPAddress.Loopback) : IPAddress.Loopback);
			}
		}

		#endregion

		public static Func<IPAddress> GetUserAddress;

		#region CurrentHandler

		/// <summary>
		/// Gets the current handler.
		/// </summary>
		/// <value>The current handler.</value>
		public static IHttpHandler CurrentHandler
		{
			get { return HttpContext.Current.Handler; }
		}

		#endregion

		#region CurrentPage

		/// <summary>
		/// Gets the current handler.
		/// </summary>
		/// <value>The current handler.</value>
		public static Page CurrentPage
		{
			get { return HttpContext.Current.Handler as Page; }
		}

		#endregion

		public static TItem GetDataItem<TItem>(this Page page)
		{
			if (page == null)
				throw new ArgumentNullException("page");

			return (TItem)page.GetDataItem();
		}

		public static string GetMimeType(this string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

			if (fileName[0] != '.')
				fileName = '.' + fileName;

			return _getMimeType.ReturnInvoke('\\' + fileName);
		}

		public static string GetMimeType(this IWebFile file)
		{
			if (file == null)
				throw new ArgumentNullException("file");

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
				throw new ArgumentNullException("url");

			HttpContext.Current.Response.Redirect(url.ToString(), endResponse);
		}

		public static void RegisterScript<T>(this T control, string key, string script)
			where T : Control
		{
			if (control == null)
				throw new ArgumentNullException("control");

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
				throw new ArgumentNullException("parent");

			var control = parent.FindControl(id);

			if (control == null)
				throw new ArgumentException("Control not founded with id '{0}'".Put(id), "id");

			control.SetValue(value);
		}

		public static void SetValue<T>(this Control control, T value)
		{
			if (control == null)
				throw new ArgumentNullException("control");

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
				throw new ArgumentException("Control with id '{0}' has unsupported type '{1}'.".Put(control.ID, control.GetType()), "control");
		}

		public static T GetChildValue<T>(this Control parent, string id)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");

			var control = parent.FindControl(id);

			if (control == null)
				throw new ArgumentException("Control not founded with id '{0}'".Put(id), "id");

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
				throw new ArgumentException("Control with id '{0}' has unsupported type '{1}'.".Put(control.ID, control.GetType()), "control");

			return value.To<T>();
		}

		public static void Download(this HttpContext context, IWebFile file)
		{
			context.Download(file, new Size<int>(), false);
		}

		public static void Download(this HttpContext context, IWebFile file, Size<int> size, bool embed)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (file == null)
				throw new ArgumentNullException("file");

			// http://stackoverflow.com/questions/994135/image-from-httphandler-wont-cache-in-browser
			var body = FormatBody(file, size);
			context.Response.ContentType = file.GetMimeType();
			context.Response.AppendHeader("Content-Disposition", "{0}; filename=\"{1}\"".Put(embed ? "inline" : "attachment", file.Name));
			context.Response.OutputStream.Write(body, 0, body.Length);
			context.Response.End();
		}

		private static byte[] FormatBody(IWebFile file, Size<int> size)
		{
			if (file == null)
				throw new ArgumentNullException("file");

			var body = file.Body.To<Stream>();

			if (size.Width > 0 && size.Height > 0)
			{
				using (var bmp = new Bitmap(body))
				{
					var coeff = (double)bmp.Width / size.Width;

					if (coeff <= 1)
						coeff = (double)bmp.Height / size.Height;

					if (coeff > 1)
					{
						using (var thumbnail = bmp.GetThumbnailImage((int)(bmp.Width / coeff), (int)(bmp.Height / coeff), () => false, IntPtr.Zero))
						{
							body = new MemoryStream();
							thumbnail.Save(body, ImageFormat.Png);
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

		public static NameValueCollection ParseUrl(this string url)
		{
			return HttpUtility.ParseQueryString(url, _urlEncoding);
		}
	}
}