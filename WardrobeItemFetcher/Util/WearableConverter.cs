using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace WardrobeItemFetcher.Util
{
    public enum WearableType
    {
        Head,
        Chest,
        Legs,
        Back
    }

    public static class WearableConverter
    {
        /// <summary>
        /// Converts the wearable into a Wardrobe-compatible JObject.
        /// This copies mandatory parameters such as the name and (fe)maleFrames, as well as the file path and name.
        /// Additional parameters may be copied if present.
        /// </summary>
        /// <param name="wearable">.head/.chest/.legs/.back raw JSON</param>
        /// <param name="assetPath">Path to the asset, ending with /, \ or with the file name and extension</param>
        /// <param name="fileName">Name of the file with extension.</param>
        /// <param name="parameters">Additional parameter names. If present in <paramref name="wearable"/>, copy it to the returned object.</param>
        /// <returns></returns>
        public static JObject Convert(JObject wearable, string assetPath, string fileName, string[] parameters = null)
        {
            JObject newWearable = new JObject();

            newWearable["path"] = Path.GetDirectoryName(assetPath).Replace("\\", "/") + "/";
            newWearable["fileName"] = fileName;

            newWearable["name"] = wearable["itemName"];
            newWearable["shortdescription"] = wearable.GetValue("shortdescription", StringComparison.OrdinalIgnoreCase); // In case shortDescription is valid and used.
            newWearable["icon"] = wearable["inventoryIcon"];
            newWearable["maleFrames"] = wearable["maleFrames"];
            newWearable["femaleFrames"] = wearable["femaleFrames"];

            var mask = wearable["mask"];
            if (mask != null) newWearable["mask"] = wearable["mask"];

            JToken colorOptions = wearable.SelectToken("colorOptions");
            if (colorOptions is JArray)
            {
                bool validColors = true;
                foreach (var item in colorOptions)
                {
                    if (item.Type != JTokenType.Object)
                    {
                        // Malformed color options.
                        validColors = false;
                        break;
                    }
                }

                if (validColors)
                {
                    newWearable["colorOptions"] = colorOptions;
                }
            }

            // Additional parameters (such as 'tags').
            if (parameters != null)
            {
                foreach (var parameterName in parameters)
                {
                    var p = wearable[parameterName];
                    if (p != null && p.Type != JTokenType.Null)
                    {
                        newWearable[parameterName] = wearable[parameterName];
                    }
                }
            }

            return newWearable;
        }
    }
}
