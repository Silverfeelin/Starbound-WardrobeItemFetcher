using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
                HandleFile(file, wearables);
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

        private static void AddPatchWearables(JArray patch, string path, JArray wearables)
        {
            foreach (JObject wearable in wearables)
            {
                patch.Add(PatchBuilder.AddOperation(path, wearable));
            }
        }

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
                HandleFile(file, wearables);
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

        private static void AddWearables(JArray array, JArray wearables)
        {
            foreach (JObject wearable in wearables)
            {
                array.Add(wearable);
            }
        }

        private static void HandleFile(FileInfo file, Dictionary<WearableType, JArray> wearables)
        {
            try
            {
                JObject wearable = JObject.Parse(File.ReadAllText(file.FullName));

                WearableType wearableType = GetWearableType(file.Extension);
                wearable = WearableConverter.Convert(wearable, wearableType);

                wearables[wearableType].Add(wearable);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Skipped file '{0}': {1}", file.FullName, exc.Message);
            }
        }

        private static string GetPatchPath(WearableType type)
        {
            switch (type)
            {
                default:
                    return string.Format("/{0}/-", type.ToString().ToLowerInvariant());
            }
        }

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
