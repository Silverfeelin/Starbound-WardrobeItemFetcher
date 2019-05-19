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
                colorOptions = FixColorOptions(colorOptions as JArray);
                if (colorOptions != null) newWearable["colorOptions"] = colorOptions;
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

        // Adds # because Starbound parses color options starting with 0 as octal numbers.
        // Returned array is a new updated color option array.
        public static JArray FixColorOptions(JArray colorOptions)
        {
            JArray newColorOptions = new JArray();

            foreach (var tColorOption in colorOptions)
            {
                if (tColorOption.Type != JTokenType.Object)
                {
                    Console.Error.WriteLine("Faulty color option found. {0}", tColorOption);
                    return null;
                }

                var colorOption = tColorOption as JObject;
                var newColorOption = new JObject();
                // Directives
                foreach (var item in colorOption)
                {
                    string key = $"#{item.Key}";
                    newColorOption[key] = item.Value;
                }

                newColorOptions.Add(newColorOption);
            }

            return newColorOptions;
        }
    }
}
