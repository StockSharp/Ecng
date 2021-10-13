namespace Ecng.Net.BBCodes
{
	using System;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Web;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Net;

	public class BB2HtmlFormatter<TContext, TDomain> : IHtmlFormatter
		where TContext : BB2HtmlContext<TDomain>
	{
		//private string _rgxBBCodeLocalizationTag;
		private readonly Regex _rgxNoParse;
		private readonly Regex _rgxBold;
		private readonly string _rgxBr;
		private readonly string _rgxBullet;
		private readonly string _rgxCenter;
		private readonly Regex _rgxCode1;
		private readonly Regex _rgxCode2;
		private readonly string _rgxList4;
		private readonly string _rgxColor;
		private readonly Regex _rgxFloat;
		private readonly Regex _rgxEmail1;
		private readonly Regex _rgxEmail2;
		private readonly string _rgxFont;
		private readonly string _rgxHr;
		private readonly Regex _rgxImg;
		private readonly Regex _rgxImgSize;
		private readonly Regex _rgxImgTitle;
		private readonly Regex _rgxImgTitleSize;
		private readonly string _rgxHighlighted;
		private readonly string _rgxItalic;
		private readonly string _rgxLeft;
		private readonly string _rgxList1;
		private readonly string _rgxList2;
		private readonly string _rgxList3;
		private readonly string _rgxPost;
		private readonly Regex _rgxQuote1;
		private readonly Regex _rgxQuote2;
		private readonly Regex _rgxQuote3;
		private readonly string _rgxRight;
		private readonly string _rgxSize;
		private readonly string _rgxStrike;
		private readonly string _rgxTopic;
		private readonly string _rgxMessage;
		private readonly string _rgxUnderline;
		private readonly Regex _rgxUrl1;
		private readonly Regex _rgxUrl2;
		//private readonly Regex _rgxUrlId1;
		//private readonly Regex _rgxUrlId2;
		private readonly ProcessReplaceRules<TContext> _instance;
		private readonly Regex _rgxModalUrl1;
		private readonly Regex _rgxModalUrl2;
		private readonly Regex _rgxYouTube;
		private readonly Regex _rgxYouTube2;
		private readonly Regex _rgxVimeo;
		private readonly Regex _rgxUser;
		private readonly Regex _rgxSpoiler;

		private readonly Regex _rgxTable;
		private readonly Regex _rgxTable2;
		private readonly Regex _rgxTable3;
		private readonly Regex _rgxTableRow;
		private readonly Regex _rgxTableCell;
		private readonly Regex _rgxTableCell2;
		private readonly Regex _rgxTableHeader;
		private readonly Regex _rgxTableHeader2;

		private readonly Regex _rgxH2;
		private readonly Regex _rgxH2Id;
		private readonly Regex _rgxH3;
		private readonly Regex _rgxH3Id;

		private readonly Regex _rgxHtml;
		private readonly Regex _rgxPackage;
		private readonly Regex _rgxProduct;

		//private const RegexOptions _options = RegexOptions.Multiline | RegexOptions.IgnoreCase;

		private const string _blank = "target=\"_blank\"";
		private const string _noFollow = "rel=\"nofollow\"";

		private readonly Func<long, TDomain, string> _getFileUrl;
		private readonly Func<long, TDomain, string> _getUserUrl;
		private readonly Func<long, TDomain, string, string> _getProductUrl;
		private readonly Func<string, TDomain, string> _getPackageFullUrl;
		private readonly Func<long, TDomain, string> _getTopicUrl;
		private readonly Func<long, TDomain, string> _getMessageUrl;
		private readonly Func<string, string> _encryptUrl;
		private readonly Func<string, TDomain, string> _toFullAbsolute;
		private readonly Func<string, TDomain, string> _getLocString;
		private readonly Func<long, IProductObject<TDomain>> _getProduct;
		private readonly Func<long, INamedObject<TDomain>> _getUser;
		private readonly Func<long, INamedObject<TDomain>> _getFile;
		private readonly Func<long, IPageObject<TDomain>> _getPage;
		private readonly Func<string, string> _generateId;
		private readonly Func<TDomain, string> _getHost;
		private readonly Func<string, (TDomain domain, bool isAway, bool noFollow)> _getUrlInfo;
		private readonly Action<TDomain, TDomain, StringBuilder> _localizeUrl;

		public BB2HtmlFormatter(
			Func<long, TDomain, string> getFileUrl,
			Func<long, TDomain, string> getUserUrl,
			Func<long, TDomain, string, string> getProductUrl,
			Func<string, TDomain, string> getPackageFullUrl,
			Func<long, TDomain, string> getTopicUrl,
			Func<long, TDomain, string> getMessageUrl,
			Func<string, string> encryptUrl,
			Func<string, TDomain, string> toFullAbsolute,
			Func<string, TDomain, string> getLocString,
			Func<long, IProductObject<TDomain>> getProduct,
			Func<long, INamedObject<TDomain>> getUser,
			Func<long, INamedObject<TDomain>> getFile,
			Func<long, IPageObject<TDomain>> getPage,
			Func<string, string> generateId,
			Func<TDomain, string> getHost,
			Func<string, (TDomain domain, bool isAway, bool noFollow)> getUrlInfo,
			Action<TDomain, TDomain, StringBuilder> localizeUrl)
		{
			_getFileUrl = getFileUrl ?? throw new ArgumentNullException(nameof(getFileUrl));
			_getUserUrl = getUserUrl ?? throw new ArgumentNullException(nameof(getUserUrl));
			_getProductUrl = getProductUrl ?? throw new ArgumentNullException(nameof(getProductUrl));
			_getPackageFullUrl = getPackageFullUrl ?? throw new ArgumentNullException(nameof(getPackageFullUrl));
			_getTopicUrl = getTopicUrl ?? throw new ArgumentNullException(nameof(getTopicUrl));
			_getMessageUrl = getMessageUrl ?? throw new ArgumentNullException(nameof(getMessageUrl));
			_encryptUrl = encryptUrl ?? throw new ArgumentNullException(nameof(encryptUrl));
			_toFullAbsolute = toFullAbsolute ?? throw new ArgumentNullException(nameof(toFullAbsolute));
			_getLocString = getLocString ?? throw new ArgumentNullException(nameof(getLocString));
			_getProduct = getProduct ?? throw new ArgumentNullException(nameof(getProduct));
			_getUser = getUser ?? throw new ArgumentNullException(nameof(getUser));
			_getFile = getFile ?? throw new ArgumentNullException(nameof(getFile));
			_getPage = getPage ?? throw new ArgumentNullException(nameof(getPage));
			_generateId = generateId ?? throw new ArgumentNullException(nameof(generateId));
			_getHost = getHost ?? throw new ArgumentNullException(nameof(getHost));
			_getUrlInfo = getUrlInfo ?? throw new ArgumentNullException(nameof(getUrlInfo));
			_localizeUrl = localizeUrl ?? throw new ArgumentNullException(nameof(localizeUrl));

			const RegexOptions compiledOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
			const RegexOptions singleLine = RegexOptions.Singleline | compiledOptions;
			const RegexOptions multiLine = RegexOptions.Multiline | compiledOptions;

			//_rgxBBCodeLocalizationTag = @"\[localization=(?<tag>[^\]]*)\](?<inner>(.+?))\[/localization\]";
			_rgxNoParse = new Regex(@"\[noparse\](?<inner>(.*?))\[/noparse\]", singleLine);
			_rgxBold = new Regex(@"\[B\](?<inner>(.*?))\[/B\]", singleLine);
			_rgxBr = "[\r]?\n(?!.*<[^>]+>.*)";
			_rgxBullet = @"\[\*\]";
			_rgxCenter = @"\[center\](?<inner>(.*?))\[/center\]";
			_rgxCode1 = new Regex(@"\[code\](?<inner>(.*?))\[/code\]", singleLine);
			_rgxCode2 = new Regex(@"\[code=(?<language>[^\]]*)\](?<inner>(.*?))\[/code\]", singleLine);
			_rgxColor = @"\[color=(?<color>(\#?[-a-z0-9]*))\](?<inner>(.*?))\[/color\]";
			_rgxFloat = new Regex(@"\[float=(?<float>[^\]]*)\](?<inner>(.*?))\[/float\]");
			_rgxEmail1 = new Regex(@"\[email[^\]]*\](?<inner>(.+?))\[/email\]", singleLine);
			_rgxEmail2 = new Regex(@"\[email=(?<email>[^\]]*)\](?<inner>(.+?))\[/email\]", singleLine);
			_rgxFont = @"\[font=(?<font>([-a-z0-9, ]*))\](?<inner>(.*?))\[/font\]";
			_rgxHr = "^[-][-][-][-][-]*[\r]?[\n]";
			_rgxImg = new Regex(@"\[img\](?<http>(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.+?))\[/img\]", singleLine);
			_rgxImgSize = new Regex(@"\[size width=(?<width>[^\]]*) height=(?<height>[^\]]*)\]\[img\](?<http>(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.+?))\[/img\]\[/size\]", singleLine);
			_rgxImgTitle = new Regex(@"\[img=(?<http>(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.+?))\](?<description>(.*?))\[/img\]", singleLine);
			_rgxImgTitleSize = new Regex(@"\[size width=(?<width>[^\]]*) height=(?<height>[^\]]*)\]\[img=(?<http>(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.+?))\](?<description>(.*?))\[/img\]\[/size\]", singleLine);
			_rgxHighlighted = @"\[h\](?<inner>(.*?))\[/h\]";
			_rgxItalic = @"\[I\](?<inner>(.*?))\[/I\]";
			_rgxLeft = @"\[left\](?<inner>(.*?))\[/left\]";
			_rgxList1 = @"\[list\](?<inner>(.*?))\[/list\]";
			_rgxList2 = @"\[list=1\](?<inner>(.*?))\[/list\]";
			_rgxList3 = @"\[list=a\](?<inner>(.*?))\[/list\]";
			_rgxList4 = @"\[list=i\](?<inner>(.*?))\[/list\]";
			_rgxPost = @"\[post=(?<post>[^\]]*)\](?<inner>(.*?))\[/post\]";
			_rgxQuote1 = new Regex(@"\[quote\](?<inner>(.*?))\[/quote\]", singleLine);
			_rgxQuote2 = new Regex(@"\[quote=(?<quote>[^\]]*)\](?<inner>(.*?))\[/quote\]", singleLine);
			_rgxQuote3 = new Regex(@"\[quote=(?<quote>(.*?));(?<id>([0-9]*))\](?<inner>(.*?))\[/quote\]", singleLine);
			_rgxRight = @"\[right\](?<inner>(.*?))\[/right\]";
			_rgxSize = @"\[size=(?<size>([1-9]))\](?<inner>(.*?))\[/size\]";
			_rgxStrike = @"\[S\](?<inner>(.*?))\[/S\]";
			_rgxTopic = @"\[topic=(?<topic>[^\]]*)\](?<inner>(.*?))\[/topic\]";
			_rgxMessage = @"\[message=(?<message>[^\]]*)\](?<inner>(.*?))\[/message\]";
			_rgxUnderline = @"\[U\](?<inner>(.*?))\[/U\]";
			_rgxUrl1 = new Regex(@"\[url\](?<http>(skype:)|(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.+?))\[/url\]", singleLine);
			_rgxUrl2 = new Regex(@"\[url\=(?<http>(skype:)|(http://)|(https://)|(ftp://)|(ftps://))?(?<url>([^\]]*?))\](?<inner>(.+?))\[/url\]", singleLine);
			//_rgxUrlId1 = new Regex(@"\[url\](?<http>(skype:)|(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.+?))\[/url\]", singleLine);
			//_rgxUrlId2 = new Regex(@"\[url\=(?<http>(skype:)|(http://)|(https://)|(ftp://)|(ftps://))?(?<url>([^\]]*?))\](?<inner>(.+?))\[/url\]", singleLine);
			_rgxModalUrl1 = new Regex(@"\[modalurl](?<http>(skype:)|(http://)|(https://)| (ftp://)|(ftps://))?(?<inner>(.+?))\[/modalurl\]", singleLine);
			_rgxModalUrl2 = new Regex(@"\[modalurl\=(?<http>(skype:)|(http://)|(https://)|(ftp://)|(ftps://))?(?<url>([^\]]*?))\](?<inner>(.+?))\[/modalurl\]", singleLine);
			_rgxYouTube = new Regex(@"\[youtube\](?<inner>(?<http>(http://)|(https://))(?<prefix>[A-Za-z][A-Za-z][A-Za-z]?\.)?youtube.com/watch\?v=(?<id>[0-9A-Za-z-_]{11}))[^[]*\[/youtube\]", singleLine);
			_rgxYouTube2 = new Regex(@"\[youtube\](?<inner>(?<http>(http://)|(https://))(?<prefix>[A-Za-z][A-Za-z][A-Za-z]?\.)?youtu.be/(?<id>[0-9A-Za-z-_]{11}))[^[]*\[/youtube\]", singleLine);
			_rgxVimeo = new Regex(@"\[vimeo\](?<inner>(?<http>(http://)|(https://))(?<prefix>[A-Za-z][A-Za-z][A-Za-z]?\.)?vimeo.com/(?<vimeoId>[0-9]{8}))[^[]*\[/vimeo\]", singleLine);
			_rgxUser = new Regex(@"\[user\](?<id>([0-9]*))\[/user\]", singleLine);
			_rgxProduct = new Regex(@"\[product\](?<id>([0-9]*))\[/product\]", singleLine);
			_rgxPackage = new Regex(@"\[package\](?<id>(.+?))\[/package\]", singleLine);
			_rgxSpoiler = new Regex(@"\[spoiler\](?<inner>.+?)\[/spoiler\]", singleLine);
			_rgxHtml = new Regex(@"\[html\](?<inner>.+?)\[/html\]", singleLine);

			_rgxTable = new Regex(@"\[table\](?<inner>(.*?))\[/table\]", singleLine);
			_rgxTable2 = new Regex(@"\[t\](?<inner>(.*?))\[/t\]", singleLine);
			_rgxTable3 = new Regex(@"\[t=(?<style>[^\]]*)\](?<inner>(.*?))\[/t\]", singleLine);
			_rgxTableRow = new Regex(@"\[tr\](?<inner>(.*?))\[/tr\]", singleLine);
			_rgxTableCell = new Regex(@"\[td\](?<inner>(.*?))\[/td\]", singleLine);
			_rgxTableCell2 = new Regex(@"\[td=(?<colspan>[^\]]*)\](?<inner>(.*?))\[/td\]", singleLine);
			_rgxTableHeader = new Regex(@"\[th\](?<inner>(.*?))\[/th\]", singleLine);
			_rgxTableHeader2 = new Regex(@"\[th=(?<color>[^\]]*)\](?<inner>(.*?))\[/th\]", singleLine);

			_rgxH2 = new Regex(@"\[h2\](?<inner>(.*?))\[/h2\]", singleLine);
			_rgxH2Id = new Regex(@"\[h2=(?<id>[^\]]*)\](?<inner>(.*?))\[/h2\]", singleLine);
			_rgxH3 = new Regex(@"\[h3\](?<inner>(.*?))\[/h3\]", singleLine);
			_rgxH3Id = new Regex(@"\[h3=(?<id>[^\]]*)\](?<inner>(.*?))\[/h3\]", singleLine);

			_instance = new ProcessReplaceRules<TContext>();

			//AddRule(new CodeRegexReplaceRule(_rgxCode1, "<div class=\"code\"><strong>{0}</strong><div class=\"innercode\">${inner}</div></div>".Replace("{0}", str6)));
			AddRule(new FontSizeRegexReplaceRule<TContext, TDomain>(_rgxSize, "<span style=\"font-size:${size}\">${inner}</span>", singleLine));
			//if (doFormatting)
			//{
			AddRule(new CodeRegexReplaceRule<TContext, TDomain>(_rgxNoParse, c => "${inner}"));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxBold, "<b>${inner}</b>"));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxStrike, "<s>${inner}</s>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxItalic, "<em>${inner}</em>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxUnderline, "<u>${inner}</u>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxHighlighted, "<span class=\"highlight\">${inner}</span>", singleLine));
			var emailRule = new VariableRegexReplaceRule<TContext, TDomain>(_rgxEmail2, "<a href=\"mailto:${email}\">${inner}</a>", new[] { "email" });
			AddRule(emailRule);
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxEmail1, "<a href=\"mailto:${inner}\">${inner}</a>") { RuleRank = emailRule.RuleRank + 1 });
			AddRule(new UrlRule(this, _rgxUrl2, "<a {0} href=\"${http}${url}\" title=\"${http}${url}\">${inner}</a>".Replace("{0}", _blank), new[] { "url", "http" }, new[] { string.Empty, "http://" }));
			AddRule(new UrlRule(this, _rgxUrl1, "<a {0} href=\"${http}${inner}\" title=\"${http}${inner}\">${http}${innertrunc}</a>".Replace("{0}", _blank), new[] { "http" }, new[] { string.Empty, "http://" }, 50));
			//AddRule(new UrlRule(_rgxUrlId2, "<a {0} href=\"${id}\" title=\"${id}\">${inner}</a>".Replace("{0}", _blank), new[] { "url", "http" }, new[] { string.Empty, "http://" }));
			//AddRule(new UrlRule(_rgxUrlId1, "<a {0} href=\"${id}\" title=\"${http}${inner}\">${http}${innertrunc}</a>".Replace("{0}", _blank), new[] { "http" }, new[] { string.Empty, "http://" }, 50));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxModalUrl2, "<a {0} {1} href=\"${http}${url}\" title=\"${http}${url}\">${inner}</a>".Replace("{0}", _blank).Replace("{1}", "class=\"ceebox\""), new[] { "url", "http" }, new[] { string.Empty, "http://" }));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxModalUrl1, "<a {0} {1} href=\"${http}${inner}\" title=\"${http}${inner}\">${http}${innertrunc}</a>".Replace("{0}", _blank).Replace("{1}", "class=\"ceebox\""), new[] { "http" }, new[] { string.Empty, "http://" }, 50));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxFont, "<span style=\"font-family:${font}\">${inner}</span>", singleLine, new[] { "font" }));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxColor, "<span style=\"color:${color}\">${inner}</span>", singleLine, new[] { "color" }));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxFloat, "<span style=\"float:${float}; padding:10px\">${inner}</span>", new[] { "float" }));
			AddRule(new SingleRegexReplaceRule<TContext, TDomain>(_rgxBullet, "<li>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxList4, "<ol type=\"i\">${inner}</ol>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxList3, "<ol type=\"a\">${inner}</ol>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxList2, "<ol>${inner}</ol>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxList1, "<ul>${inner}</ul>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxCenter, "<div align=\"center\">${inner}</div>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxLeft, "<div align=\"left\">${inner}</div>", singleLine));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxRight, "<div align=\"right\">${inner}</div>", singleLine));
			AddRule(new ImageRule(this, _rgxImgSize, "<img src=\"${http}${inner}\" style=\"width:${width};height=${height}\" alt=\"\"/>", new[] { "http", "width", "height" }, new[] { "http://", "auto", "auto" }) { RuleRank = 70 });
			AddRule(new ImageRule(this, _rgxImg, "<img src=\"${http}${inner}\" alt=\"\"/>", new[] { "http" }, new[] { "http://" }) { RuleRank = 71 });
			AddRule(new ImageRule(this, _rgxImgTitle, "<img src=\"${http}${inner}\" alt=\"${description}\" title=\"${description}\" />", new[] { "http", "description" }, new[] { "http://", string.Empty }) { RuleRank = 73 });
			AddRule(new ImageRule(this, _rgxImgTitleSize, "<img src=\"${http}${inner}\" style=\"width:${width};height=${height}\" alt=\"${description}\" title=\"${description}\" />", new[] { "http", "width", "height", "description" }, new[] { "http://", "auto", "auto", string.Empty }) { RuleRank = 72 });
			//AddRule(new VariableRegexReplaceRule(_rgxImg, "<a class=\"thumbnail\" href=\"#thumb\"><img style=\"max-width:400px\" src=\"${http}${inner}\" alt=\"\"/><span><img src=\"${http}${inner}\" alt=\"\"/></span></a>", new[] { "http" }, new[] { "http://" }));
			//AddRule(new VariableRegexReplaceRule(_rgxImgTitle, "<a class=\"thumbnail\" href=\"#thumb\"><img style=\"max-width:400px\" src=\"${http}${inner}\" alt=\"\"/><span><img src=\"${http}${inner}\" alt=\"${description}\" title=\"${description}\" /><br/>${description}</span></a>", new string[] { "http", "description" }, new string[] { "http://" }));
			AddRule(new RemoveNewLineRule(_rgxTable, "<table border='0' cellspacing='2' cellpadding='5'>${inner}</table>", false));
			AddRule(new RemoveNewLineRule(_rgxTable2, "<table border='0' cellspacing='2' cellpadding='5'>${inner}</table>", false));
			AddRule(new RemoveNewLineRule(_rgxTable3, "<table style='${style}'>${inner}</table>", true));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxTableRow, "<tr>${inner}</tr>"));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxTableCell, "<td>${inner}</td>"));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxTableCell2, "<td colspan=${colspan}>${inner}</td>", new[] { "colspan" }));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxTableHeader, "<th>${inner}</th>"));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxTableHeader2, "<th bgcolor=${color}>${inner}</th>", new[] { "color" }));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxH2, "<h2>${inner}</h2>"));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxH2Id, "<a href=#${id}><h2 id=${id}>${inner}</h2></a>", new[] { "id" }));
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxH3, "<h3>${inner}</h3>"));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxH3Id, "<a href=#${id}><h3 id=${id}>${inner}</h3></a>", new[] { "id" }));
			var newRule = new SingleRegexReplaceRule<TContext, TDomain>(_rgxHr, "<hr />", multiLine);
			var brRule = new SingleRegexReplaceRule<TContext, TDomain>(_rgxBr, "<br />", multiLine) { RuleRank = newRule.RuleRank + 1 };
			AddRule(newRule);
			AddRule(brRule);
			//}
			var smiles = new[]
			{
				new { Icon = "angry.svg", Emoticon = "Angry", Code = "[angry]" },
				new { Icon = "happy.svg", Emoticon = "BigGrin", Code = "[biggrin]" },
				new { Icon = "msp_blink.gif", Emoticon = "Blink", Code = "[blink]" },
				new { Icon = "msp_blushing.gif", Emoticon = "Blushing", Code = "[blush]" },
				new { Icon = "msp_bored.gif", Emoticon = "Bored", Code = "[bored]" },
				new { Icon = "confused.svg", Emoticon = "Confused", Code = "[confused]" },
				new { Icon = "msp_cool.gif", Emoticon = "Cool", Code = "[cool]" },
				new { Icon = "unhappy.svg", Emoticon = "Crying", Code = "[crying]" },
				new { Icon = "msp_cursing.gif", Emoticon = "Cursing", Code = "[cursing]" },
				new { Icon = "msp_drool.gif", Emoticon = "Drool", Code = "[drool]" },
				new { Icon = "msp_flapper.gif", Emoticon = "Flapper", Code = "[flapper]" },
				new { Icon = "msp_glare.gif", Emoticon = "Glare", Code = "[glare]" },
				new { Icon = "msp_huh.gif", Emoticon = "Huh", Code = "[huh]" },
				new { Icon = "smiling.svg", Emoticon = "Laugh", Code = "[laugh]" },
				new { Icon = "lol.svg", Emoticon = "LOL", Code = "[lol]" },
				new { Icon = "in-love.svg", Emoticon = "Love", Code = "[love]" },
				new { Icon = "msp_mad.gif", Emoticon = "Mad", Code = "[mad]" },
				new { Icon = "msp_mellow.gif", Emoticon = "Mellow", Code = "[mellow]" },
				new { Icon = "msp_ohmy.gif", Emoticon = "OhMyGod", Code = "[omg]" },
				new { Icon = "msp_rolleyes.gif", Emoticon = "RollEyes", Code = "[rolleyes]" },
				new { Icon = "msp_sad.gif", Emoticon = "Sad", Code = "[sad]" },
				new { Icon = "msp_scared.gif", Emoticon = "Scared", Code = "[scared]" },
				new { Icon = "msp_sleep.gif", Emoticon = "Sleep", Code = "[sleep]" },
				new { Icon = "smile.svg", Emoticon = "Smile", Code = "[smile]" },
				new { Icon = "msp_sneaky.gif", Emoticon = "Sneaky", Code = "[sneaky]" },
				new { Icon = "msp_thumbdn.gif", Emoticon = "ThumbDown", Code = "[thumbdn]" },
				new { Icon = "msp_thumbup.gif", Emoticon = "ThumpUp", Code = "[thumbup]" },
				new { Icon = "msp_tongue.gif", Emoticon = "Tongue", Code = "[tongue]" },
				new { Icon = "msp_razz.gif", Emoticon = "Razz", Code = "[razz]" },
				new { Icon = "msp_unsure.gif", Emoticon = "Unsure", Code = "[unsure]" },
				new { Icon = "msp_w00t.gif", Emoticon = "Woot", Code = "[woot]" },
				new { Icon = "wink.svg", Emoticon = "Wink", Code = "[wink]" },
				new { Icon = "msp_wub.gif", Emoticon = "Wub", Code = "[wub]" },
				new { Icon = "smart.svg", Emoticon = "Smart", Code = "[smart]" },
				new { Icon = "ninja.svg", Emoticon = "Ninja", Code = "[ninja]" },
				new { Icon = "nerd.svg", Emoticon = "Nerd", Code = "[nerd]" },
				new { Icon = "quiet.svg", Emoticon = "Quiet", Code = "[quiet]" },
				new { Icon = "ill.svg", Emoticon = "Ill", Code = "[ill]" },
				new { Icon = "happy.svg", Emoticon = "Happy", Code = "[happy]" },
				new { Icon = "suspicious.svg", Emoticon = "Suspicious", Code = "[suspicious]" },
			};

			var codeOffset = 0;

			foreach (var smile in smiles)
			{
				var code = smile.Code;

				code = code.Replace("&", "&amp;");
				code = code.Replace(">", "&gt;");
				code = code.Replace("<", "&lt;");
				code = code.Replace("\"", "&quot;");

				var alt = smile.Emoticon.EncodeToHtml();

				Func<TDomain, string> replace = domain =>
				{
					var src = _toFullAbsolute(Path.GetExtension(smile.Icon).EqualsIgnoreCase(".gif")
						? $"~/images/smiles/{smile.Icon}"
						: $"~/images/svg/smiles/{smile.Icon}", domain);

					return $"<img src=\"{src}\" alt=\"{alt}\" class=\"smiles\" />";
				};

				// add new rules for smilies...
				var lowerRule = new SimpleReplaceRule<TContext, TDomain>(code.ToLower(), replace);
				var upperRule = new SimpleReplaceRule<TContext, TDomain>(code.ToUpper(), replace);

				// increase the rank as we go...
				lowerRule.RuleRank = lowerRule.RuleRank + 100 + codeOffset;
				upperRule.RuleRank = upperRule.RuleRank + 100 + codeOffset;

				AddRule(lowerRule);
				AddRule(upperRule);

				// add a bit more rank
				codeOffset++;
			}
			//if (convertBBQuotes)
			//{
			AddRule(new SyntaxHighlightedCodeRegexReplaceRule<TContext, TDomain>(_rgxCode2, langCode => "<div class=\"code\"><strong>{0}</strong><div class=\"innercode\">${inner}</div></div>".Replace("{0}", _getLocString("Code", langCode))) { RuleRank = 41 });
			AddRule(new CodeRegexReplaceRule<TContext, TDomain>(_rgxCode1, langCode =>  "<div class=\"code\"><strong>{0}</strong><div class=\"innercode\">${inner}</div></div>".Replace("{0}", _getLocString("Code", langCode))));

			//ForumPage page = new ForumPage();
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxQuote2, "<div class=\"quote\"><span class=\"quotetitle\">{0}</span><div class=\"innerquote\">{1}</div></div>".Put("${quote}", "${inner}"), new[] { "quote" }) { RuleRank = 63 });
			AddRule(new SimpleRegexReplaceRule<TContext, TDomain>(_rgxQuote1, langCode => "<div class=\"quote\"><span class=\"quotetitle\">{0}</span><div class=\"innerquote\">{1}</div></div>".Put($"{_getLocString("Quote", langCode)}:", "${inner}")) { RuleRank = 64 });
			AddRule(new VariableRegexReplaceRuleEx(this, _rgxQuote3, langCode => "<div class=\"quote\"><span class=\"quotetitle\">{0} <a href=\"{1}\"><img src=\"{2}\" title=\"{3}\" alt=\"{3}\" /></a></span><div class=\"innerquote\">{4}</div></div>".Put("${quote}", _toFullAbsolute("~/posts/m/${id}/", langCode), _toFullAbsolute("~/images/icon_latest_reply.gif", langCode), _getLocString("GoTo", langCode), "${inner}"), new[] { "quote", "id" }) { RuleRank = 62 });
			//}
			AddRule(new TopicRegexReplaceRule(this, _rgxPost, "<a {0} href=\"${post}\">${inner}</a>".Replace("{0}", _blank), singleLine));
			AddRule(new TopicRegexReplaceRule(this, _rgxTopic, "<a {0} href=\"${topic}\">${inner}</a>".Replace("{0}", _blank), singleLine));
			AddRule(new TopicRegexReplaceRule(this, _rgxMessage, "<a {0} href=\"${message}\">${inner}</a>".Replace("{0}", _blank), singleLine));

			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxYouTube, "<iframe width=\"640\" height=\"390\" src=\"//www.youtube.com/embed/${id}\" frameborder=\"0\" allowfullscreen></iframe>", new[] { "id" }));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxYouTube2, "<iframe width=\"640\" height=\"390\" src=\"//www.youtube.com/embed/${id}\" frameborder=\"0\" allowfullscreen></iframe>", new[] { "id" }));
			AddRule(new VariableRegexReplaceRule<TContext, TDomain>(_rgxVimeo, "<iframe width=\"560\" height=\"350\" src=\"https://player.vimeo.com/video/${vimeoId}?show_title=1&show_byline=1&show_portrait=1&&fullscreen=1\" frameborder=\"0\"></iframe>", new[] { "prefix", "vimeoId" }));
			AddRule(new FacebookRule(new Regex(@"\[fb\]https:\/\/www.facebook.com\/(?<innerUrl>.+)\[/fb\]", singleLine)));

			AddRule(new UserRule(this, _rgxUser, "<a href='${url}'>${name}</a>"));
			AddRule(new PackageRule(this, _rgxPackage, "<a href='${url}'>${name}</a>"));
			AddRule(new ProductRule(this, _rgxProduct, "<a href='${url}'>${name}</a>"));
			AddRule(new Product2Rule(this));

			AddRule(new SpoilerRule(this, _rgxSpoiler));
			AddRule(new HtmlRule(_rgxHtml));

			AddRule(new VariableRegexReplaceRuleEx(this, new Regex(@"(?<before>^|[ ]|\>|\[[A-Za-z0-9]\])(?<inner>(([A-Za-z0-9]+_+)|([A-Za-z0-9]+\-+)|([A-Za-z0-9]+\.+)|([A-Za-z0-9]+\++))*[A-Za-z0-9]+@((\w+\-+)|(\w+\.))*\w{1,63}\.[a-zA-Z]{2,6})", multiLine),
				"${before}<a href=\"mailto:${inner}\">${inner}</a>", new[] { "before" })
			{
				RuleRank = 10
			});
			
			AddRule(new VariableRegexReplaceRuleEx(this, new Regex("(?<before>^|[ ]|\\>|\\[[A-Za-z0-9]\\])(?<!href=\")(?<!src=\")(?<inner>(http://|https://|ftp://)(?:[\\w-]+\\.)+[\\w-]+(?:/[\\w-./?+%#&=;:,]*)?)", multiLine),
				"${before}<a {0} {1} href=\"${inner}\" title=\"${inner}\">${innertrunc}</a>".Replace("{0}", _blank).Replace("{1}", _noFollow), new[] { "before" }, new[] { string.Empty }, 50)
			{
				RuleRank = 10
			});
			AddRule(new VariableRegexReplaceRuleEx(this, new Regex("(?<before>^|[ ]|\\>|\\[[A-Za-z0-9]\\])(?<!href=\")(?<!src=\")(?<inner>(http://|https://|ftp://)(?:[\\w-]+\\.)+[\\w-]+(?:/[\\w-./?%&=+;,:#~$]*[^.<|^.\\[])?)", multiLine),
				"${before}<a {0} {1} href=\"${inner}\" title=\"${inner}\">${innertrunc}</a>".Replace("{0}", _blank).Replace("{1}", _noFollow), new[] { "before" }, new[] { string.Empty }, 50)
			{
				RuleRank = 10
			});
			AddRule(new VariableRegexReplaceRuleEx(this, new Regex(@"(?<before>^|[ ]|\>|\[[A-Za-z0-9]\])(?<!http://)(?<inner>www\.(?:[\w-]+\.)+[\w-]+(?:/[\w-./?%+#&=;,]*)?)", multiLine),
				"${before}<a {0} {1} href=\"http://${inner}\" title=\"http://${inner}\">${innertrunc}</a>".Replace("{0}", _blank).Replace("{1}", _noFollow), new[] { "before" }, new[] { string.Empty }, 50)
			{
				RuleRank = 10
			});

			AddRule(new DynamicPageRule(this));
		}

		public void AddRule(IReplaceRule<TContext> rule)
		{
			_instance.AddRule(rule);
		}

		private static void ReplaceSchema(StringBuilder builder, TContext context)
		{
			if (context.Scheme == "http")
				builder.ReplaceIgnoreCase("https://", "http://");
			else if (context.Scheme == "https")
				builder.ReplaceIgnoreCase("http://", "https://");
		}

		private class VariableRegexReplaceRuleEx : VariableRegexReplaceRule<TContext, TDomain>
		{
			private readonly Func<TDomain, string> _getRegExReplace;
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public VariableRegexReplaceRuleEx(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace, string[] variables, string[] varDefaults, int truncateLength)
				: base(regExSearch, regExReplace, variables, varDefaults, truncateLength)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public VariableRegexReplaceRuleEx(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace, string[] variables)
				: base(regExSearch, regExReplace, variables)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public VariableRegexReplaceRuleEx(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, Func<TDomain, string> getRegExReplace, string[] variables)
				: base(regExSearch, null, variables)
			{
				_getRegExReplace = getRegExReplace ?? throw new ArgumentNullException(nameof(getRegExReplace));
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var sb = new StringBuilder(text);
				var domain = context.Domain;

				var m = RegExSearch.Match(text);
				while (m.Success)
				{
					var innerReplace = new StringBuilder((_getRegExReplace ?? RegExReplace).Invoke(domain));
					var i = 0;

					foreach (var tVar in Variables)
					{
						var varName = tVar;
						var handlingValue = string.Empty;

						if (varName.Contains(":"))
						{
							// has handling section
							var tmpSplit = varName.Split(':');
							varName = tmpSplit[0];
							handlingValue = tmpSplit[1];
						}

						var tValue = m.Groups[varName].Value;

						if (VariableDefaults != null && tValue.Length == 0)
						{
							// use default instead
							tValue = VariableDefaults[i];
						}

						innerReplace.Replace("${" + varName + "}", ManageVariableValue(context, varName, tValue, handlingValue));
						i++;
					}

					innerReplace.Replace("${inner}", m.Groups["inner"].Value);

					if (TruncateLength > 0)
					{
						// special handling to truncate urls
						innerReplace.Replace(
						  "${innertrunc}", m.Groups["inner"].Value.TruncateMiddle(TruncateLength));
					}

					var (urlDomain, isAway, noFollow) = _parent._getUrlInfo(innerReplace.ToString());

					if (!urlDomain.IsDefault())
					{
						innerReplace.Replace(_blank + " ", string.Empty);

						if (!context.IsUrlLocalizeDisabled)
						{
							_parent._localizeUrl(urlDomain, domain, innerReplace);
							ReplaceSchema(innerReplace, context);
						}

						if (noFollow)
							innerReplace.Replace(_noFollow + " ", string.Empty);
					}

					if (isAway)
					{
						var str = innerReplace.ToString();
						var start = str.IndexOfIgnoreCase("href=") + 6;
						var end = str.IndexOfIgnoreCase("\"", start);
						innerReplace.Remove(start, end - start);
						innerReplace.Insert(start, $"{context.Scheme}://{_parent._getHost(domain)}/away/?u={_parent._encryptUrl(str.Substring(start, end - start))}");
					}

					// pulls the htmls into the replacement collection before it's inserted back into the main text
					replacement.ReplaceHtmlFromText(ref innerReplace, cancellationToken);

					var @group = m.Groups[0];

					// remove old bbcode...
					sb.Remove(@group.Index, @group.Length);

					// insert replaced value(s)
					sb.Insert(@group.Index, innerReplace.ToString());

					// text = text.Substring( 0, m.Groups [0].Index ) + tStr + text.Substring( m.Groups [0].Index + m.Groups [0].Length );
					m = RegExSearch.Match(sb.ToString());
				}

				return Task.FromResult(sb.ToString());
			}
		}

		public async Task<string> ToHtmlAsync(string text, TContext context, CancellationToken cancellationToken = default)
		{
			if (text.IsEmpty())
				return text;

			text = RepairHtml(text);

			return await _instance.ProcessAsync(context, text, cancellationToken);
		}

		private static string RepairHtml(string html)
		{
			var matchs = Regex.Matches(html, "[^\r]\n[^\r]", RegexOptions.IgnoreCase);

			for (var i = matchs.Count - 1; i >= 0; i--)
				html = html.Insert(matchs[i].Index + 1, " \r");

			var matchs2 = Regex.Matches(html, "[^\r]\n\r\n[^\r]", RegexOptions.IgnoreCase);

			for (var j = matchs2.Count - 1; j >= 0; j--)
				html = html.Insert(matchs2[j].Index + 1, " \r");

			return html.EncodeToHtml();
		}

		private class UrlRule : VariableRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public UrlRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace, string[] variables, string[] varDefaults, int truncateLength)
				: base(regExSearch, regExReplace, variables, varDefaults, truncateLength)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public UrlRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace, string[] variables, string[] varDefaults)
				: base(regExSearch, regExReplace, variables, varDefaults)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));
					var index = 0;

					var url = match.Groups["url"].Value;
					var inner = match.Groups["inner"].Value;

					var hasTitle = !url.IsEmpty();
					url = hasTitle ? url : inner;

					var isId = long.TryParse(url, out var id);

					if (!isId)
					{
						foreach (var str in Variables)
						{
							var variableName = str;
							var handlingValue = string.Empty;

							if (variableName.Contains(":"))
							{
								var strArray = variableName.Split(':');
								variableName = strArray[0];
								handlingValue = strArray[1];
							}

							var variableValue = match.Groups[variableName].Value;
							if ((VariableDefaults != null) && (variableValue.Length == 0))
							{
								variableValue = VariableDefaults[index];
							}

							sb.Replace("${" + variableName + "}", ManageVariableValue(context, variableName, variableValue, handlingValue));
							index++;
						}
					}
					else
						sb.Replace("${http}", string.Empty);

					if (isId)
					{
						var file = _parent._getFile(id);

						url = _parent._toFullAbsolute(_parent._getFileUrl(file?.Id ?? id, domain), domain);

						if (hasTitle)
							sb.Replace("${url}", url);
						else
							inner = url;

						if (file?.GetName(domain).IsImage() == true)
						{
							sb.Replace("title=", $"data-preview-id='{id}' title=");
						}
					}

					sb.Replace("${inner}", inner);

					if (TruncateLength > 0)
					{
						sb.Replace("${innertrunc}", match.Groups["inner"].Value.TruncateMiddle(TruncateLength));
					}

					if (!isId)
					{
						var (urlDomain, away, noFollow) = _parent._getUrlInfo(sb.ToString());

						if (!urlDomain.IsDefault())
						{
							sb.Replace(_blank + " ", string.Empty);

							if (!context.IsUrlLocalizeDisabled)
							{
								_parent._localizeUrl(urlDomain, domain, sb);
								ReplaceSchema(sb, context);
							}
						}

						sb.Replace("/forum/resource.ashx?a", "/file.aspx?t=forum&fid");

						if (away)
						{
							var str = sb.ToString();
							var start = str.IndexOfIgnoreCase("href=") + 6;
							var end = str.IndexOfIgnoreCase("\"", start);
							sb.Remove(start, end - start);
							sb.Insert(start, $"{context.Scheme}://{_parent._getHost(domain)}/away/?u={_parent._encryptUrl(str.Substring(start, end - start))}");
						}
						else if (noFollow)
						{
							var str = sb.ToString();
							var start = str.IndexOfIgnoreCase("href=");
							sb.Insert(start, _noFollow + " ");
						}
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class SpoilerRule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;
			
			public SpoilerRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch)
				: base(regExSearch, "<div class='spoilertitle'>${inner}</div>")
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);

				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));

					var id = _parent._generateId("spolier");

					sb.Replace("${inner}", $@"<input type='button' value='{_parent._getLocString("ShowSpoiler", domain)}' class='btn btn-primary' onclick=""toggleSpoiler(this, '{id}');"" title='{_parent._getLocString("ShowSpoiler", domain)}' /></div><div class='spoilerbox' id='{id}' style='display:none'>" + match.Groups["inner"].Value);

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var @group = match.Groups[0];
					builder.Remove(@group.Index, @group.Length);
					builder.Insert(@group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class HtmlRule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			public HtmlRule(Regex regExSearch)
				: base(regExSearch, "<span>${inner}</span>")
			{
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));

					var html = match.Groups["inner"].Value;

					if (context.AllowHtml)
						html = HttpUtility.HtmlDecode(html);

					sb.Replace("${inner}", html);

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var g = match.Groups[0];
					builder.Remove(g.Index, g.Length);
					builder.Insert(g.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class FacebookRule : VariableRegexReplaceRule<TContext, TDomain>
		{
			public FacebookRule(Regex regex)
				: base(regex, "<iframe src=\"https://www.facebook.com/plugins/post.php?href=${url}&width=500&show_text=true&height=573&appId\" width=\"500\" height=\"573\" style=\"border:none;overflow:hidden\" scrolling=\"no\" frameborder=\"0\" allowfullscreen=\"true\" allow=\"autoplay; clipboard-write; encrypted-media; picture-in-picture; web-share\"></iframe>", new[] { "innerUrl" })
			{
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));
					var url = match.Groups["innerUrl"].Value;

					sb.Replace("${url}", $"https://www.facebook.com/{url}".EncodeUrl());

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class UserRule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public UserRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace)
				: base(regExSearch, regExReplace)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);

				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));
					var idStr = match.Groups["id"].Value;

					if (long.TryParse(idStr, out var id))
					{
						var client = _parent._getUser(id);

						sb.Replace("${url}", _parent._toFullAbsolute(_parent._getUserUrl(client.Id, domain), domain));
						sb.Replace("${name}", client.GetName(domain)?.CheckUrl());
					}
					else
					{
						sb.Replace("${url}", idStr);
						sb.Replace("${name}", idStr);
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class PackageRule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public PackageRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace)
				: base(regExSearch, regExReplace)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));

					var packageId = match.Groups["id"].Value;

					if (!packageId.IsEmpty())
					{
						sb.Replace("${url}", _parent._getPackageFullUrl(packageId, domain));
						sb.Replace("${name}", packageId);
					}
					else
					{
						sb.Replace("${url}", string.Empty);
						sb.Replace("${name}", string.Empty);
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class ProductRule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public ProductRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace)
				: base(regExSearch, regExReplace)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);

				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));

					var idStr = match.Groups["id"].Value;

					if (long.TryParse(idStr, out var id))
					{
						var product = _parent._getProduct(id);

						sb.Replace("${url}", _parent._toFullAbsolute(_parent._getProductUrl(product.Id, domain, product.GetUrlPart(domain)), domain));
						sb.Replace("${name}", product.GetName(domain));
					}
					else
					{
						sb.Replace("${url}", idStr);
						sb.Replace("${name}", idStr);
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class Product2Rule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public Product2Rule(BB2HtmlFormatter<TContext, TDomain> parent)
				: base(new Regex(@"\[product=(?<id>([0-9]*))\](?<inner>(.*?))\[/product\]", RegexOptions.Singleline | RegexOptions.IgnoreCase), "<a href='${url}'>${inner}</a>")
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));

					var idStr = match.Groups["id"].Value;
					var inner = match.Groups["inner"].Value;

					if (long.TryParse(idStr, out var id))
					{
						var product = _parent._getProduct(id);

						if (product != null)
						{
							sb.Replace("${url}", _parent._toFullAbsolute(_parent._getProductUrl(product.Id, domain, product.GetUrlPart(domain)), domain));

							if (inner.IsEmpty())
								inner = product.GetName(domain);
						}
						else
							sb.Replace("${url}", idStr);

						if (inner.IsEmpty())
							inner = idStr;

						sb.Replace("${inner}", inner);
					}
					else
					{
						if (inner.IsEmpty())
							inner = idStr;

						sb.Replace("${url}", idStr);
						sb.Replace("${inner}", inner);
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class DynamicPageRule : SimpleRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public DynamicPageRule(BB2HtmlFormatter<TContext, TDomain> parent)
				: base(new Regex(@"\[page=(?<id>([0-9]*))\](?<inner>(.*?))\[/page\]", RegexOptions.Singleline | RegexOptions.IgnoreCase), "<a href='${url}'>${inner}</a>")
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));

					var idStr = match.Groups["id"].Value;
					var inner = match.Groups["inner"].Value;

					if (long.TryParse(idStr, out var id))
					{
						var page = _parent._getPage(id);

						if (page != null)
						{
							sb.Replace("${url}", _parent._toFullAbsolute(page.GetUrlPart(domain), domain));

							if (inner.IsEmpty())
								inner = page.GetHeader(domain);
						}
						else
							sb.Replace("${url}", idStr);

						if (inner.IsEmpty())
							inner = idStr;

						sb.Replace("${inner}", inner);
					}
					else
					{
						if (inner.IsEmpty())
							inner = idStr;

						sb.Replace("${url}", idStr);
						sb.Replace("${inner}", inner);
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class ImageRule : VariableRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public ImageRule(BB2HtmlFormatter<TContext, TDomain> parent, Regex regExSearch, string regExReplace, string[] variables, string[] varDefaults)
				: base(regExSearch, regExReplace, variables, varDefaults)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);
				var domain = context.Domain;

				for (var match = RegExSearch.Match(text); match.Success; match = RegExSearch.Match(builder.ToString()))
				{
					var sb = new StringBuilder(RegExReplace(domain));
					var index = 0;

					var imgUrl = match.Groups["inner"].Value;
					var isId = long.TryParse(imgUrl, out var fileId);

					foreach (var str in Variables)
					{
						var variableName = str;
						var handlingValue = string.Empty;

						if (variableName.Contains(":"))
						{
							var strArray = variableName.Split(':');
							variableName = strArray[0];
							handlingValue = strArray[1];
						}

						if (isId && variableName == "http")
							continue;

						var variableValue = match.Groups[variableName].Value;
						if ((VariableDefaults != null) && (variableValue.Length == 0))
						{
							variableValue = VariableDefaults[index];
						}

						sb.Replace("${" + variableName + "}", ManageVariableValue(context, variableName, variableValue, handlingValue));
						index++;
					}

					var http = match.Groups["http"].Value;

					if (!isId && !imgUrl.IsEmpty() && !context.IsUrlLocalizeDisabled)
					{
						var (urlDomain, _, _) = _parent._getUrlInfo(imgUrl);

						if (!urlDomain.IsDefault())
						{
							var imgUrlBuilder = new StringBuilder(imgUrl);
							_parent._localizeUrl(urlDomain, domain, imgUrlBuilder);
							ReplaceSchema(imgUrlBuilder, context);
							imgUrl = imgUrlBuilder.ToString();
						}
					}

					var size = "500x500";

					var widthGroup = match.Groups["width"];
					var heightGroup = match.Groups["height"];

					if (widthGroup.Success && heightGroup.Success)
						size = widthGroup.Value.Replace("px", string.Empty) + "x" + heightGroup.Value.Replace("px", string.Empty);

					if (isId)
					{
						var file = _parent._getFile(fileId);

						var url = _parent._toFullAbsolute(_parent._getFileUrl(fileId, domain), domain);

						var fileName = file?.GetName(domain);
						var isGif = Path.GetExtension(fileName).EqualsIgnoreCase(".gif");

						if (!context.PreventScaling)
						{
							var style = isGif ? "style='max-width: 600px;' " : string.Empty;
							sb.Insert(0, $"<a href='{url}' class='lightview' {style}data-lightview-options=\"skin: 'mac'\" data-lightview-group='mixed'>");
							sb.Append("</a>");

							if (!isGif)
								url += "?size=" + size;
						}

						sb.Replace("${description}", fileName);
						sb.Replace("${inner}", url);
						sb.Replace("${http}", string.Empty);
					}
					else
						sb.Replace("${inner}", imgUrl);

					if (TruncateLength > 0)
					{
						sb.Replace("${innertrunc}", imgUrl.TruncateMiddle(TruncateLength));
					}
					
					if (!isId)
					{
						if (http.IsEmpty())
							http = "http://";

						var sbStr = sb.ToString();

						if ((sbStr.ContainsIgnoreCase("/forum/resource.ashx?i=")
						     || sbStr.ContainsIgnoreCase("/forum/resource.ashx?a=")
						     || sbStr.ContainsIgnoreCase("file.aspx")))
						{
							const string baseImgUrl = "/file.aspx?t=forum";

							if (!context.PreventScaling)
							{
								if (sbStr.ContainsIgnoreCase("file.aspx"))
									sb.Replace("/file.aspx?", $"/file.aspx?&size={size}&amp;");
								else
								{
									sb
										.Replace("/forum/resource.ashx?i", baseImgUrl + $"&size={size}&amp;fid")
										.Replace("/forum/resource.ashx?a", baseImgUrl + $"&size={size}&amp;fid");
								}

								sb.Insert(0, $"<a href='{http}{imgUrl}' class='lightview' data-lightview-options=\"skin: 'mac'\" data-lightview-group='mixed'>");
							}

							sb
								.Replace("/forum/resource.ashx?i", baseImgUrl + "&fid")
								.Replace("/forum/resource.ashx?a", baseImgUrl + "&fid");

							if (!context.PreventScaling)
								sb.Append("</a>");
						}
						else
						{
							if (!context.PreventScaling)
							{
								sb.ReplaceIgnoreCase("alt=", "style='max-width: 600px;' alt=");

								sb.Insert(0, $"<a href='{http}{imgUrl}' class='lightview' data-lightview-options=\"skin: 'mac'\" data-lightview-group='mixed'>");
								sb.Append("</a>");
							}
						}
					}

					replacement.ReplaceHtmlFromText(ref sb, cancellationToken);

					var group = match.Groups[0];
					builder.Remove(group.Index, group.Length);
					builder.Insert(group.Index, sb.ToString());
				}

				return Task.FromResult(builder.ToString());
			}
		}

		private class RemoveNewLineRule : BaseReplaceRule<TContext>
		{
			private readonly string _regExReplace;
			private readonly Regex _regExSearch;
			private readonly bool _hasStyle;

			public override string RuleDescription => $"RegExSearch = \"{_regExSearch}\"";

			public RemoveNewLineRule(string regExSearch, string regExReplace, RegexOptions regExOptions)
			{
				_regExSearch = new Regex(regExSearch, regExOptions);
				_regExReplace = regExReplace;
			}

			public RemoveNewLineRule(Regex regExSearch, string regExReplace, bool hasStyle)
			{
				_regExSearch = regExSearch;
				_regExReplace = regExReplace;
				_hasStyle = hasStyle;
			}

			public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
			{
				var builder = new StringBuilder(text);

				for (var match = _regExSearch.Match(text); match.Success; match = _regExSearch.Match(builder.ToString()))
				{
					var strText = _regExReplace.Replace("${inner}", GetInnerValue(match.Groups["inner"].Value.Remove(Environment.NewLine)));
					
					if (_hasStyle)
						strText = strText.Replace("${style}", GetInnerValue(match.Groups["style"].Value.Remove(Environment.NewLine)));

					replacement.ReplaceHtmlFromText(ref strText, cancellationToken);
					
					var g = match.Groups[0];

					builder.Remove(g.Index, g.Length);
					builder.Insert(g.Index, strText);
				}

				return Task.FromResult(builder.ToString());
			}

			protected virtual string GetInnerValue(string innerValue)
			{
				return innerValue;
			}
		}

		private class TopicRegexReplaceRule : VariableRegexReplaceRule<TContext, TDomain>
		{
			private readonly BB2HtmlFormatter<TContext, TDomain> _parent;

			public TopicRegexReplaceRule(BB2HtmlFormatter<TContext, TDomain> parent, string regExSearch, string regExReplace, RegexOptions regExOptions)
				: base(regExSearch, regExReplace, regExOptions, new[] { "post", "topic", "message" })
			{
				RuleRank = 200;
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			protected override string ManageVariableValue(TContext context, string variableName, string variableValue, string handlingValue)
			{
				if (variableName == "post" || variableName == "topic" || variableName == "message")
				{
					if (long.TryParse(variableValue, out var id))
					{
						var domain = context.Domain;

						switch (variableName)
						{
							case "post":
							case "message":
								return _parent._toFullAbsolute(_parent._getMessageUrl(id, domain), domain);
							case "topic":
								return _parent._toFullAbsolute(_parent._getTopicUrl(id, domain), domain);
						}
					}
				}

				return variableValue;
			}
		}

		async Task<string> IHtmlFormatter.ToHtmlAsync(string text, object context, CancellationToken cancellationToken)
		{
			return await ToHtmlAsync(text, (TContext)context, cancellationToken);
		}

		public Task<string> CleanAsync(string text, CancellationToken cancellationToken = default)
		{
			if (!text.IsEmptyOrWhiteSpace())
			{
				// process message... clean html, strip html, remove bbcode, etc...
				text = text
					.CleanHtmlString()
					.StripHtml()
					.StripBBCode()
					.RemoveMultipleWhitespace();
			}

			return Task.FromResult(text);
		}
	}
}