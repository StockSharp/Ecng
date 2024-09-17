namespace Ecng.Compilation.Roslyn;

using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers;

public static class BannedApiExtensions
{
	private class BannedSymbolsAdditionalText(string content) : AdditionalText
	{
		private readonly string _content = content;

		public override string Path => "BannedSymbols.txt";

		public override SourceText GetText(CancellationToken cancellationToken)
			=> SourceText.From(_content);
	}

	public static (DiagnosticAnalyzer analyzer, AdditionalText bannedSymbolsTxt) ToBannedSymbolsAnalyzer(this string bannedSymbols)
		=> (new CSharpSymbolIsBannedAnalyzer(), new BannedSymbolsAdditionalText(bannedSymbols));
}