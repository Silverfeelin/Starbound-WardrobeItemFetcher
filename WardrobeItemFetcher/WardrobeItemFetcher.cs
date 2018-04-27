using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using WardrobeItemFetcher.Fetcher;
using WardrobeItemFetcher.Util;

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

        #region Object

        /// <summary>
        /// Creates a wearables JSON object using the configured fetcher.
        /// </summary>
        /// <param name="fetcher">Item fetcher.</param>
        /// <param name="path">Path to file or directory to fetch from.</param>
        /// <returns>Wearables JSON object.</returns>
        public static JObject CreateObject(IFetcher fetcher, string path, bool namesOnly = false)
        {
            var wearables = CreateWearableList();

            if (namesOnly)
                fetcher.OnItemFound += (assetPath, assetData) => HandleItemName(assetPath, assetData, wearables);
            else
                fetcher.OnItemFound += (assetPath, assetData) => HandleItem(assetPath, assetData, wearables);

            fetcher.Fetch(path);

            return CreateObjectFromWearables(wearables);
        }

        /// <summary>
        /// Creates a wearables JSON object from grouped wearables per type.
        /// </summary>
        /// <param name="wearables">Grouped wearables.</param>
        /// <returns>Wearables JSON object.</returns>
        private static JObject CreateObjectFromWearables(Dictionary<WearableType, JArray> wearables)
        {
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
            foreach (JToken wearable in wearables)
            {
                array.Add(wearable);
            }
        }

        #endregion

        #region Patch

        /// <summary>
        /// Creates a wearables JSON patch using the configured fetcher.
        /// </summary>
        /// <param name="fetcher">Item fetcher.</param>
        /// <param name="path">Path to file or directory to fetch from.</param>
        /// <returns>Wearables JSON patch.</returns>
        public static JArray CreatePatch(IFetcher fetcher, string path, bool namesOnly = false)
        {
            var wearables = CreateWearableList();

            if (namesOnly)
                fetcher.OnItemFound += (assetPath, assetData) => HandleItemName(assetPath, assetData, wearables);
            else
                fetcher.OnItemFound += (assetPath, assetData) => HandleItem(assetPath, assetData, wearables);

            fetcher.Fetch(path);

            return CreatePatchFromWearables(wearables);
        }

        /// <summary>
        /// Creates a wearables patch from grouped wearables per type.
        /// </summary>
        /// <param name="wearables">Grouped wearables.</param>
        /// <returns>Wearables patch.</returns>
        private static JArray CreatePatchFromWearables(Dictionary<WearableType, JArray> wearables)
        {
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
            foreach (JToken wearable in wearables)
            {
                patch.Add(PatchBuilder.AddOperation(path, wearable));
            }
        }

        #endregion

        /// <summary>
        /// Adds an item to the proper wearables array.
        /// </summary>
        /// <param name="path">Asset path (including file name).</param>
        /// <param name="item">Item json.</param>
        /// <param name="wearables">Wearables to add wearable item to.</param>
        private static void HandleItem(string path, string item, Dictionary<WearableType, JArray> wearables)
        {
            try
            {
                JObject wearable = JObject.Parse(item);
                WearableType wearableType = GetWearableType(Path.GetExtension(path));
                wearable = WearableConverter.Convert(wearable, wearableType, path.Substring(path.LastIndexOf("/") + 1), path.Substring(path.LastIndexOf("/") + 1));

                wearables[wearableType].Add(wearable);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Skipped file '{0}': {1}", path, exc.Message);
            }
        }

        /// <summary>
        /// Adds an item name to the proper wearables array.
        /// </summary>
        /// <param name="path">Asset path used to determine item type.</param>
        /// <param name="item">Item json.</param>
        /// <param name="wearables">Wearables to add the item name to.</param>
        private static void HandleItemName(string path, string item, Dictionary<WearableType, JArray> wearables)
        {
            try
            {
                JObject wearable = JObject.Parse(item);
                WearableType wearableType = GetWearableType(Path.GetExtension(path));

                string itemName = wearable["itemName"].Value<string>();
                wearables[wearableType].Add(itemName);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Skipped file '{0}': {1}", path, exc.Message);
            }
        }
        
        #region Helper

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

        #endregion
    }
}
