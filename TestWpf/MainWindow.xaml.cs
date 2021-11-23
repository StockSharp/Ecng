using System;
using System.Text.RegularExpressions;
using System.IO;

using Ecng.Net.BBCodes;
using Ecng.Common;

using Nito.AsyncEx;

namespace TestWpf
{
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			var bb = @"[h2=text_format]Text format[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]noparse[/td]
[td]Block any BB parsing inside[/td]
[td][noparse][b]This text not parsed[/b][/noparse][/td]
[td][noparse][b]This text not parsed[/b][/noparse][/td]
[/tr]
[tr]
[td]b[/td]
[td]Bold font[/td]
[td][noparse][b]This text is bold[/b][/noparse][/td]
[td][b]This text is bold[/b][/td]
[/tr]
[tr]
[td]i[/td]
[td]Italic font[/td]
[td][noparse][i]This text is italic[/i][/noparse][/td]
[td][i]This text is italic[/i][/td]
[/tr]
[tr]
[td]font[/td]
[td]Font family[/td]
[td][noparse][font=cursive]Cursive text[/font][/noparse][/td]
[td][font=cursive]Cursive text[/font][/td]
[/tr]
[tr]
[td]color[/td]
[td]Text color[/td]
[td][noparse][color=red]Red text[/color][/noparse][/td]
[td][color=red]Red text[/color][/td]
[/tr]
[tr]
[td]size[/td]
[td]Text size[/td]
[td][noparse][size=10]This text has 7 size[/size][/noparse][/td]
[td][size=7]This text has 7 size[/size][/td]
[/tr]
[tr]
[td]highlight[/td]
[td]Text highlight[/td]
[td][noparse][h]This text highlighted[/h][/noparse][/td]
[td][h]This text highlighted[/h][/td]
[/tr]
[/t]

[h2=text_align]Text align[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]float[/td]
[td]Floating text[/td]
[td][noparse][float=right]This text floated to right[/float][/noparse][/td]
[td][float=right]This text floated to right[/float][/td]
[/tr]
[tr]
[td]left[/td]
[td]Align to left[/td]
[td][noparse][left]This text aligned to left[/left][/noparse][/td]
[td][left]This text aligned to left[/left][/td]
[/tr]
[tr]
[td]right[/td]
[td]Align to right[/td]
[td][noparse][right]This text aligned to right[/right][/noparse][/td]
[td][right]This text aligned to right[/right][/td]
[/tr]
[tr]
[td]center[/td]
[td]Align to center[/td]
[td][noparse][center]This text aligned to center[/center][/noparse][/td]
[td][center]This text aligned to center[/center][/td]
[/tr]
[tr]
[td]code[/td]
[td]Code block[/td]
[td][noparse][code]var i = 10;[/code][/noparse][/td]
[td][code]var i = 10;[/code][/td]
[/tr]
[tr]
[td]code[/td]
[td]Code block[/td]
[td][noparse][code=c#]var i = 10;[/code][/noparse][/td]
[td][code=c#]var i = 10;[/code][/td]
[/tr]
[/t]

[h2=links]Links[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]url[/td]
[td]Link[/td]
[td][noparse][url]https://stocksharp.com/[/url][/noparse][/td]
[td][url]https://stocksharp.com/[/url][/td]
[/tr]
[tr]
[td]url[/td]
[td]Link with text[/td]
[td][noparse][url=https://stocksharp.com/]StockSharp Web site[/url][/noparse][/td]
[td][url=https://stocksharp.com/]StockSharp Web site[/url][/td]
[/tr]
[tr]
[td]email[/td]
[td]Email link[/td]
[td][noparse][email]info@stocksharp.com[/email][/noparse][/td]
[td][email]info@stocksharp.com[/email][/td]
[/tr]
[tr]
[td]email[/td]
[td]Email link with text[/td]
[td][noparse][email=info@stocksharp.com]Send me[/email][/noparse][/td]
[td][email=info@stocksharp.com]Send me[/email][/td]
[/tr]
[tr]
[td]topic[/td]
[td]Topic link by id[/td]
[td][noparse][topic=12374]Link to topic[/topic][/noparse][/td]
[td][topic=12374]Link to topic[/topic][/td]
[/tr]
[tr]
[td]message[/td]
[td]Message link by id[/td]
[td][noparse][message=51317]Link to message[/message][/noparse][/td]
[td][message=51317]Link to message[/message][/td]
[/tr]
[tr]
[td]product[/td]
[td]Product link by id[/td]
[td][noparse][product]9[/product][/noparse][/td]
[td][product]9[/product][/td]
[/tr]
[tr]
[td]product[/td]
[td]Product link by id with text[/td]
[td][noparse][product=9]Link to S#.Designer[/product][/noparse][/td]
[td][product=9]Link to S#.Designer[/product][/td]
[/tr]
[tr]
[td]user[/td]
[td]User link by id[/td]
[td][noparse][user]1[/user][/noparse][/td]
[td][user]1[/user][/td]
[/tr]
[/t]

[h2=images]Images[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]img[/td]
[td]Image by url[/td]
[td][noparse][img]https://stocksharp.com/file/122179/cup_svg/[/img][/noparse][/td]
[td][img]https://stocksharp.com/file/122179/cup_svg/[/img][/td]
[/tr]
[tr]
[td]img[/td]
[td]Image by url with tooltip[/td]
[td][noparse][img=https://stocksharp.com/file/122179/cup_svg/]This is custom tooltip.[/img][/noparse][/td]
[td][img=https://stocksharp.com/file/122179/cup_svg/]This is custom tooltip.[/img][/td]
[/tr]
[tr]
[td]img[/td]
[td]Image by id[/td]
[td][noparse][img]122179[/img][/noparse][/td]
[td][img]122179[/img][/td]
[/tr]
[tr]
[td]img[/td]
[td]Image by id with tooltip[/td]
[td][noparse][img=122179]This is custom tooltip.[/img][/noparse][/td]
[td][img=122179]This is custom tooltip.[/img][/td]
[/tr]
[tr]
[td]img size[/td]
[td]Image with fixed size[/td]
[td][noparse]
[size width=10px height=10px][img]https://stocksharp.com/file/122179/cup_svg/[/img][/size]
[size width=10px height=10px][img]122179[/img][/size]
[size width=10px height=10px][img=122179]This is custom tooltip.[/img][/size]
[/noparse][/td]
[td]
[size width=10px height=10px][img]https://stocksharp.com/file/122179/cup_svg/[/img][/size]
[size width=10px height=10px][img]122179[/img][/size]
[size width=10px height=10px][img=122179]This is custom tooltip.[/img][/size]
[/td]
[/tr]
[/t]

[h2=lists]Lists[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]list[/td]
[td]List with bullet[/td]
[td][noparse]
[list]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/noparse][/td]
[td]
[list]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/td]
[/tr]
[tr]
[td]list[/td]
[td]List with number[/td]
[td][noparse]
[list=1]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/noparse][/td]
[td]
[list=1]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/td]
[/tr]
[tr]
[td]list[/td]
[td]List with letter[/td]
[td][noparse]
[list=a]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/noparse][/td]
[td]
[list=a]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/td]
[/tr]
[tr]
[td]list[/td]
[td]List with latin[/td]
[td][noparse]
[list=i]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/noparse][/td]
[td]
[list=i]
[*]Item 1
[*]Item 2
[*]Item 3
[/list]
[/td]
[/tr]
[/t]

[h2=social]Social[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]youtube[/td]
[td]YouTube[/td]
[td][noparse][youtube]https://youtu.be/lsbyvqajlZ8[/youtube][/noparse][/td]
[td][youtube]https://youtu.be/lsbyvqajlZ8[/youtube][/td]
[/tr]
[tr]
[td]vimeo[/td]
[td]Vimeo[/td]
[td][noparse][vimeo]https://vimeo.com/66733743[/vimeo][/noparse][/td]
[td][vimeo]https://vimeo.com/66733743[/vimeo][/td]
[/tr]
[tr]
[td]fb[/td]
[td]Facebook[/td]
[td][noparse][fb]https://www.facebook.com/stocksharp/posts/4554987014515465[/fb][/noparse][/td]
[td][fb]https://www.facebook.com/stocksharp/posts/4554987014515465[/fb][/td]
[/tr]
[/t]

[role=133362]
[h2=extended]Extended (allowed only for administrators)[/h2]
[t=width:100%]
[th]BB code[/th]
[th]Description[/th]
[th]Syntax[/th]
[th]Example[/th]
[tr]
[td]page[/td]
[td]Page link by id[/td]
[td][noparse][page=239]Link to store FAQ[/page][/noparse][/td]
[td][page=239]Link to store FAQ[/page][/td]
[/tr]
[tr]
[td]page[/td]
[td]Page link by id[/td]
[td][noparse][page=239][/page][/noparse][/td]
[td][page=239][/page][/td]
[/tr]
[tr]
[td]html[/td]
[td]Embedded html code[/td]
[td][noparse][html]any html code here[/html][/noparse][/td]
[td][html]<blockquote class=""twitter-tweet""><p lang=""en"" dir=""ltr"">Hi all!<br><br>Do you know what benefits and knowledge you can get from our training in algorithmic training course?<br>- Create your own trading robot<br>- Real made working examples of trading strategies<br>- Training chat<br><br>Follow the link below to learn more!<a href=""https://t.co/YpsKldZ2GO"">https://t.co/YpsKldZ2GO</a> <a href=""https://t.co/U1hxgfY3nc"">pic.twitter.com/U1hxgfY3nc</a></p>&mdash; StockSharp (@StockSharp) <a href=""https://twitter.com/StockSharp/status/1383028699780554759?ref_src=twsrc%5Etfw"">April 16, 2021</a></blockquote> <script async src=""https://platform.twitter.com/widgets.js"" charset=""utf-8""></script>[/html][/td]
[/tr]
[tr]
[td]role[/td]
[td]Hidden for public access text[/td]
[td][noparse][role=133362]This text visible for content managers only[/role][/noparse][/td]
[td][role=133362]This text visible for content managers only[/role][/td]
[/tr]
[/t]
[/role]

[img=107130]TerminalBlackEn.png[/img]";

			var ctx = CreateContext(true, false);
			var html = AsyncContext.Run(() => CreateBBService().ToHtmlAsync(bb, ctx));
			HtmlCtrl.Text = $"<html>{html}</html>";
		}

		private class TextBB2HtmlContext : BB2HtmlContext
		{
			public TextBB2HtmlContext(bool preventScaling, bool allowHtml, string scheme, string domainCode, bool localhost)
				: base(preventScaling, allowHtml, scheme)
			{
				DomainCode = domainCode;
				Localhost = localhost;
			}

			public string DomainCode { get; }
			public bool Localhost { get; }
		}

		private class NamedObjectImpl : INamedObject<TextBB2HtmlContext>
		{
			public long Id { get; set; }
			public string Name { get; set; }
			public string PackageId => Name;

			public string UrlPart { get; set; }

			string INamedObject<TextBB2HtmlContext>.GetName(TextBB2HtmlContext ctx) => Name;
			string INamedObject<TextBB2HtmlContext>.GetUrlPart(TextBB2HtmlContext ctx) => UrlPart;
		}

		private static TextBB2HtmlContext CreateContext(bool allowHtml, bool localhost)
			=> new(false, allowHtml, Uri.UriSchemeHttps, "com", localhost);

		private static readonly Regex _isStockSharpCom = new("href=\"(?<http>(http://)|(https://))?(\\w+.)?stocksharp.com", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex _isStockSharpRu = new("href=\"(?<http>(http://)|(https://))?(\\w+.)?stocksharp.ru", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex _isGitHub = new("href=\"https://github.com/stocksharp", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static BB2HtmlFormatter<TextBB2HtmlContext> CreateBBService()
		{
			static string GetPackageLink(TextBB2HtmlContext ctx, string packageId)
			{
				if (packageId.IsEmpty())
					throw new ArgumentNullException(nameof(packageId));

				return $"https://www.nuget.org/packages/{packageId}/";
			}

			static string ToFullAbsolute(TextBB2HtmlContext ctx, string virtualPath)
			{
				var domain = ctx.Localhost ? "http://localhost/stocksharp" : $"{ctx.Scheme}://stocksharp.{ctx.DomainCode}";

				if (virtualPath.StartsWithIgnoreCase("http"))
				{ }
				else if (virtualPath.StartsWith("/"))
					virtualPath = $"{domain}{virtualPath}";
				else
					virtualPath = virtualPath.Replace("~", domain);

				return virtualPath;
			}

			static INamedObject<TextBB2HtmlContext> GetProduct(long id)
			{
				//var idStr = urlPart.IsEmpty() ? id.To<string>() : urlPart.ToLowerInvariant();
				//return $"~/store/{idStr}/";

				return id switch
				{
					9 => new NamedObjectImpl { Id = id, Name = "S#.Designer", UrlPart = "~/store/strategy designer" },
					_ => throw new ArgumentOutOfRangeException(nameof(id)),
				};
			}

			static INamedObject<TextBB2HtmlContext> GetFile(long id)
				=> new NamedObjectImpl { Id = id, UrlPart = $"~/file/{id}" };

			static INamedObject<TextBB2HtmlContext> GetTopic(long id)
				=> new NamedObjectImpl { Id = id, UrlPart = $"~/topic/{id}" };

			static INamedObject<TextBB2HtmlContext> GetMessage(long id)
				=> new NamedObjectImpl { Id = id, UrlPart = $"~/posts/m/{id}" };

			static INamedObject<TextBB2HtmlContext> GetUser(long id)
			{
				return id switch
				{
					1 => new NamedObjectImpl { Id = id, Name = "StockSharp", UrlPart = $"~/users/{id}/" },
					_ => throw new ArgumentOutOfRangeException(nameof(id)),
				};
			}

			static INamedObject<TextBB2HtmlContext> GetPage(long id)
			{
				return id switch
				{
					239 => new NamedObjectImpl { Id = id, Name = "Using S#.Installer to publish user content", UrlPart = "~/store/faq/" },
					_ => throw new ArgumentOutOfRangeException(nameof(id)),
				};
			}

			var bb = new BB2HtmlFormatter<TextBB2HtmlContext>(
				GetProduct,
				GetUser,
				GetFile,
				GetTopic,
				GetMessage,
				GetPage,

				GetPackageLink,
				(ctx, s) => s,
				ToFullAbsolute,
				(ctx, key) =>
				{
					return key switch
					{
						"ShowSpoiler" => "Show spoiler",
						"Code" => "Code",
						_ => throw new ArgumentOutOfRangeException(nameof(key)),
					};
				},
				(ctx, sourceUrl) => $"{sourceUrl}_a6f78c5fce344124993798c028a22a3a",
				ctx => $"stocksharp.{ctx.DomainCode}",
				(ctx, url) =>
				{
					string domain = null;

					var urlStr = url.ToString();

					Match match;

					if ((match = _isStockSharpCom.Match(urlStr)).Success)
						domain = "com";
					else if ((match = _isStockSharpRu.Match(urlStr)).Success)
						domain = "ru";

					var changed = !domain.IsEmpty() && (ctx.Localhost || domain != ctx.DomainCode);
					if (changed)
					{
						url.ReplaceIgnoreCase($"{match.Groups["http"]}stocksharp.{domain}", ToFullAbsolute(ctx, "~"));
					}

					var isGitHub = _isGitHub.IsMatch(urlStr);
					var isAway = domain is null && !isGitHub;
					var noFollow = isAway;
					var isBlank = isAway || isGitHub;
					return (changed, isAway, noFollow, isBlank);
				},
				(ctx, url) => url.UrlEscape(),
				(ctx, img) =>
				{
					return Path.GetExtension(img).EqualsIgnoreCase(".gif")
						? $"~/images/smiles/{img}"
						: $"~/images/svg/smiles/{img}"
					;
				});

			//bb.AddRule(new VideoStockSharpReplaceRule());
			//bb.AddRule(new RoleRule());

			return bb;
		}
	}
}