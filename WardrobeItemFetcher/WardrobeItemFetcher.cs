using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WardrobeItemFetcher
{
    public static class WardrobeItemFetcher
    {
        private static Dictionary<WearableType, JArray> CreateWearableList()
        {
            Dictionary<WearableType, JArray> wearables = new Dictionary<WearableType, JArray>();
            wearables[WearableType.Head] = new JArray();
            wearables[WearableType.Chest] = new JArray();
            wearables[WearableType.Legs] = new JArray();
            wearables[WearableType.Back] = new JArray();

            return wearables;
        }

        /// <summary>
        /// Creates a wearables patch file by finding all wearables in the given directory recursively.
        /// </summary>
        /// <param name="directory">Asset directory.</param>
        /// <returns>Wearable patch.</returns>
        public static JArray CreatePatch(DirectoryInfo directory)
        {
            Dictionary<WearableType, JArray> wearables = CreateWearableList();

            // Fetch files
            FileFetcher fileFetcher = new FileFetcher()
            {
                Extensions = new HashSet<string>() { "head", "chest", "legs", "back" }
            };

            fileFetcher.OnFileFound += (file =>
            {
                HandleFile(directory, file, wearables);
            });

            fileFetcher.Fetch(directory, true);

            // Create patch for found wearables
            JArray patch = new JArray();

            foreach (WearableType type in Enum.GetValues(typeof(WearableType)))
            {
                if (wearables.ContainsKey(type))
                    AddPatchWearables(patch, GetPatchPath(type), wearables[type]);
            }

            return patch;
        }

        /// <summary>
        /// Creates a wearables patch file by finding all wearables in the given zip archive.
        /// </summary>
        /// <param name="archive">Zip archive.</param>
        /// <returns>Wearables patch.</returns>
        public static JArray CreatePatch(ZipArchive archive)
        {
            Dictionary<WearableType, JArray> wearables = CreateWearableList();

            // Fetch files
            ArchiveFetcher fetcher = new ArchiveFetcher()
            {
                Extensions = new HashSet<string>() { "head", "chest", "legs", "back" }
            };

            ZipArchiveEntry metadata = archive.Entries.Where(e =>
            {
                var name = e.Name.ToLowerInvariant();
                return name == "_metadata" || name == ".metadata";
            }).FirstOrDefault();
            string root = metadata != null ? Path.GetDirectoryName(metadata.FullName) : "/";

            fetcher.OnEntryFound += (entry =>
            {
                HandleEntry(entry, root, wearables);
            });

            fetcher.Fetch(archive);

            // Create patch for found wearables
            JArray patch = new JArray();

            foreach (WearableType type in Enum.GetValues(typeof(WearableType)))
            {
                if (wearables.ContainsKey(type))
                    AddPatchWearables(patch, GetPatchPath(type), wearables[type]);
            }

            return patch;
        }

        /// <summary>
        /// Adds all wearables of a wearable type through patches.
        /// </summary>
        /// <param name="patch">Patch object.</param>
        /// <param name="path">Patch path. For example, /head/-</param>
        /// <param name="wearables">Wearables to add to this path.</param>
        private static void AddPatchWearables(JArray patch, string path, JArray wearables)
        {
            foreach (JObject wearable in wearables)
            {
                patch.Add(PatchBuilder.AddOperation(path, wearable));
            }
        }

        /// <summary>
        /// Creates a wearables file by finding all wearables in the given directory recursively.
        /// </summary>
        /// <param name="directory">Asset directory.</param>
        /// <returns>Wearable object.</returns>
        public static JObject CreateObject(DirectoryInfo directory)
        {
            Dictionary<WearableType, JArray> wearables = CreateWearableList();

            // Fetch files
            FileFetcher fileFetcher = new FileFetcher()
            {
                Extensions = new HashSet<string>() { "head", "chest", "legs", "back" }
            };

            fileFetcher.OnFileFound += (file =>
            {
                HandleFile(directory, file, wearables);
            });

            fileFetcher.Fetch(directory, true);

            // Create object for found wearables.
            JObject obj = new JObject(
                new JProperty("head", new JArray()),
                new JProperty("chest", new JArray()),
                new JProperty("legs", new JArray()),
                new JProperty("back", new JArray())
            );

            AddWearables(obj["head"] as JArray, wearables[WearableType.Head]);
            AddWearables(obj["chest"] as JArray, wearables[WearableType.Chest]);
            AddWearables(obj["legs"] as JArray, wearables[WearableType.Legs]);
            AddWearables(obj["back"] as JArray, wearables[WearableType.Back]);

            return obj;
        }

        /// <summary>
        /// Creates a wearables file by finding all wearables in the given zip archive.
        /// </summary>
        /// <param name="archive">Opened zip archive.</param>
        /// <returns>Wearable object.</returns>
        public static JObject CreateObject(ZipArchive archive)
        {
            Dictionary<WearableType, JArray> wearables = CreateWearableList();
            
            ArchiveFetcher fetcher = new ArchiveFetcher()
            {
                Extensions = new HashSet<string>() { "head", "chest", "legs", "back" }
            };

            ZipArchiveEntry metadata = archive.Entries.Where(e =>
            {
                var name = e.Name.ToLowerInvariant();
                return name == "_metadata" || name == ".metadata";
            }).FirstOrDefault();
            string root = metadata != null ? Path.GetDirectoryName(metadata.FullName) : "/";

            fetcher.OnEntryFound += (entry) =>
            {
                HandleEntry(entry, root, wearables);
            };

            fetcher.Fetch(archive);

            // Create object for found wearables.
            JObject obj = new JObject(
                new JProperty("head", new JArray()),
                new JProperty("chest", new JArray()),
                new JProperty("legs", new JArray()),
                new JProperty("back", new JArray())
            );

            AddWearables(obj["head"] as JArray, wearables[WearableType.Head]);
            AddWearables(obj["chest"] as JArray, wearables[WearableType.Chest]);
            AddWearables(obj["legs"] as JArray, wearables[WearableType.Legs]);
            AddWearables(obj["back"] as JArray, wearables[WearableType.Back]);

            return obj;
        }

        /// <summary>
        /// Adds the wearables from from wearables into array (merge).
        /// </summary>
        /// <param name="array">Target array.</param>
        /// <param name="wearables">Wearables to add.</param>
        private static void AddWearables(JArray array, JArray wearables)
        {
            foreach (JObject wearable in wearables)
            {
                array.Add(wearable);
            }
        }

        /// <summary>
        /// Handles a file, by parsing it and adding it to the right wearable array in the dictionary.
        /// </summary>
        /// <param name="assetDir">Asset directory, used to build the asset path.</param>
        /// <param name="file">Wearable file.</param>
        /// <param name="wearables">Output wearables.</param>
        private static void HandleFile(DirectoryInfo assetDir, FileInfo file, Dictionary<WearableType, JArray> wearables)
        {
            try
            {
                JObject wearable = JObject.Parse(File.ReadAllText(file.FullName));

                WearableType wearableType = GetWearableType(file.Extension);
                wearable = WearableConverter.Convert(wearable, wearableType, FileFetcher.AssetPath(assetDir.FullName, file.FullName), file.Name);

                wearables[wearableType].Add(wearable);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Skipped file '{0}': {1}", file.FullName, exc.Message);
            }
        }
        
        /// <summary>
        /// Handles a zip entry, by opening it, parsing the contents and adding the wearable to the right array in wearables.
        /// </summary>
        /// <param name="entry">Wearable entry.</param>
        /// <param name="root">Root asset folder path.</param>
        /// <param name="wearables">Output wearables.</param>
        private static void HandleEntry(ZipArchiveEntry entry, string root, Dictionary<WearableType, JArray> wearables)
        {
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream, true))
            {
                try
                {
                    string s = reader.ReadToEnd();
                    JObject wearable = JObject.Parse(s);

                    WearableType wearableType = GetWearableType(Path.GetExtension(entry.FullName).Substring(1));
                    wearable = WearableConverter.Convert(wearable, wearableType, ArchiveFetcher.AssetPath(root, entry.FullName), entry.Name);

                    wearables[wearableType].Add(wearable);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Skipped file '{0}': {1}", entry.FullName, exc.Message);
                }
            }
        }

        /// <summary>
        /// Gets the path for a new patch operation to add a wearable.
        /// </summary>
        /// <param name="type">Wearable type.</param>
        /// <returns>Patch path.</returns>
        private static string GetPatchPath(WearableType type)
        {
            switch (type)
            {
                default:
                    return string.Format("/{0}/-", type.ToString().ToLowerInvariant());
            }
        }

        /// <summary>
        /// Gets the wearable type from the given file extension.
        /// Extension can contain a dot, and is case-insensitive.
        /// </summary>
        /// <param name="extension">File extension.</param>
        /// <returns>Wearable type based on extension.</returns>
        private static WearableType GetWearableType(string extension)
        {
            string e = extension.ToLowerInvariant().Replace(".", "");
            switch (e)
            {
                case "head":
                    return WearableType.Head;
                case "chest":
                    return WearableType.Chest;
                case "legs":
                    return WearableType.Legs;
                case "back":
                    return WearableType.Back;
            }

            throw new ArgumentException($"Wearable type could not be determined for extension '{extension}'.");
        }
    }
}
