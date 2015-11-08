namespace Ecng.Data.SqlServer
{
	#region Using Directives

	using System;
	using System.Text.RegularExpressions;
	using System.Data.SqlClient;

	using Microsoft.SqlServer.Server;

	#endregion

	public static class SqlExtendedProcedures
	{
		#region Private Fields

		private static readonly Regex _regex = new Regex(@"@\w*(?x)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace/* | RegexOptions.Compiled*/);

		#endregion

		[SqlProcedure]
		public static void PageSelect(string table, long startIndex, long count, string orderByColumn, string columns, string filter, object param1, object param2, object param3, object param4, object param5, object param6, object param7, object param8, object param9, object param10)
		{
			if (string.IsNullOrEmpty(table))
				throw new ArgumentNullException(nameof(table));

			if (startIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (string.IsNullOrEmpty(columns))
				columns = string.Format("[{0}].*", table);

			if (string.IsNullOrEmpty(orderByColumn))
				orderByColumn = "Id";

			string countFilter;

			if (count > 0)
			{
				countFilter = "between @StartIndex and @StartIndex + @Count";
				count--;
			}
			else if (count == -1)
			{
				countFilter = ">= @StartIndex";
			}
			else
				throw new ArgumentOutOfRangeException(nameof(count));

			var command = new SqlCommand(string.Format(@"
				with Numbered as (
					select row_number() over(order by [{0}].{1}) N_Row, {3} from [{0}] {2}
				)
				select * from Numbered
					where N_Row - 1 {4}
				order by N_Row", table, orderByColumn, filter, columns, countFilter));

			command.Parameters.AddWithValue("@StartIndex", startIndex);

			if (count != -1)
				command.Parameters.AddWithValue("@Count", count);

			if (!string.IsNullOrEmpty(filter))
			{
				var externalParams = new[] { param1, param2, param3, param4, param5, param6, param7, param8, param9, param10 };

				int index = 0;
				foreach (Match match in _regex.Matches(filter))
				{
					if (!command.Parameters.Contains(match.Value))
						command.Parameters.AddWithValue(match.Value, externalParams[index++]);
				}
			}

			using (new SqlConnection("context connection = true"))
				SqlContext.Pipe.ExecuteAndSend(command);
		}
	}
}