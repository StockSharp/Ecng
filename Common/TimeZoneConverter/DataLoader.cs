namespace Ecng.Common.TimeZoneConverter
{
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;

	static class DataLoader
	{
		public static void Populate(
			IDictionary<string, string> ianaMap,
			IDictionary<string, string> windowsMap)
		{
			var mapping = GetEmbeddedData("Ecng.Common.TimeZoneConverter.Data.Mapping.csv.gz");
			var aliases = GetEmbeddedData("Ecng.Common.TimeZoneConverter.Data.Aliases.csv.gz");

			var links = new Dictionary<string, string>();
			foreach (var link in aliases)
			{
				var parts = link.Split(',');

				foreach (var key in parts[1].Split())
					links.Add(key, parts[0]);
			}

			foreach (var item in mapping)
			{
				var parts = item.Split(',');
				var windowsZone = parts[0];
				var territory = parts[1];
				var ianaZones = parts[2].Split();

				// Create the Windows map entry
				if (!links.TryGetValue(ianaZones[0], out var value))
					value = ianaZones[0];

				var key = $"{territory}|{windowsZone}";
				windowsMap.Add(key, value);

				// Create the IANA map entries
				foreach (var ianaZone in ianaZones)
				{
					if (!ianaMap.ContainsKey(ianaZone))
						ianaMap.Add(ianaZone, windowsZone);
				}
			}

			// Expand the IANA map to include all links
			foreach (var link in links)
			{
				if (ianaMap.ContainsKey(link.Key))
					continue;

				ianaMap.Add(link.Key, ianaMap[link.Value]);
			}
		}

		private static IEnumerable<string> GetEmbeddedData(string resourceName)
		{
			using (var compressedStream = typeof(DataLoader).Assembly.GetManifestResourceStream(resourceName))
			using (var stream = new GZipStream(compressedStream, CompressionMode.Decompress))
			using (var reader = new StreamReader(stream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					yield return line;
				}
			}
		}
	}
}