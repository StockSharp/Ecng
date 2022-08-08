namespace Ecng.Nuget
{
	using System.Linq;

	using NuGet.Packaging;

	public static class Extensions
	{
		public static string[] GetTargetFrameworks(this PackageArchiveReader reader)
		{
			var targetFrameworks = reader
				.GetSupportedFrameworks()
				.Select(f => f.GetShortFolderName())
				.ToList();

			// Default to the "any" framework if no frameworks were found.
			if (targetFrameworks.Count == 0)
			{
				targetFrameworks.Add("any");
			}

			return targetFrameworks.ToArray();
		}
	}
}
