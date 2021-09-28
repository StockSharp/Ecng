﻿using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace TimeZoneConverter
{
    internal static class DataLoader
    {
        public static void Populate(IDictionary<string, string> ianaMap, IDictionary<string, string> windowsMap, IDictionary<string, string> railsMap, IDictionary<string, IList<string>> inverseRailsMap)
        {
            var mapping = GetEmbeddedData("Mapping.csv.gz");
            var aliases = GetEmbeddedData("Aliases.csv.gz");
            var railsMapping = GetEmbeddedData("RailsMapping.csv.gz");

            var links = new Dictionary<string, string>();
            foreach (var link in aliases)
            {
                var parts = link.Split(',');
                var value = parts[0];
                foreach (var key in parts[1].Split())
                    links.Add(key, value);
            }
            
            var similarIanaZones = new Dictionary<string, IList<string>>();
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

                if (ianaZones.Length > 1)
                {
                    foreach (var ianaZone in ianaZones)
                        similarIanaZones.Add(ianaZone, ianaZones.Except(new[] {ianaZone}).ToArray());
                }
            }

            // Expand the IANA map to include all links
            foreach (var link in links)
            {
                if (ianaMap.ContainsKey(link.Key))
                    continue;

                ianaMap.Add(link.Key, ianaMap[link.Value]);
            }

            foreach (var item in railsMapping)
            {
                var parts = item.Split(',');
                var railsZone = parts[0].Trim('"');
                var ianaZone = parts[1].Trim('"');
                railsMap.Add(railsZone, ianaZone);
            }

            foreach (var grouping in railsMap.GroupBy(x => x.Value, x => x.Key))
            {
                inverseRailsMap.Add(grouping.Key, grouping.ToList());
            }

            // Expand the Inverse Rails map to include similar IANA zones
            foreach (var ianaZone in ianaMap.Keys)
            {
                if (inverseRailsMap.ContainsKey(ianaZone) || links.ContainsKey(ianaZone))
                    continue;

                if (similarIanaZones.TryGetValue(ianaZone, out var similarZones))
                {
                    foreach (var otherZone in similarZones)
                    {
                        if (inverseRailsMap.TryGetValue(otherZone, out var railsZones))
                        {
                            inverseRailsMap.Add(ianaZone, railsZones);
                            break;
                        }
                    }
                }
            }

            // Expand the Inverse Rails map to include links
            foreach (var link in links)
            {
                if (inverseRailsMap.ContainsKey(link.Key))
                    continue;

                if (inverseRailsMap.TryGetValue(link.Value, out var railsZone))
                    inverseRailsMap.Add(link.Key, railsZone);
            }

            
        }

        private static IEnumerable<string> GetEmbeddedData(string resourceName)
        {
            var assembly = typeof(DataLoader).GetTypeInfo().Assembly;

            using (var compressedStream = assembly.GetManifestResourceStream("Ecng.Common.TimeZoneConverter.Data." + resourceName) ?? throw new MissingManifestResourceException())
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
