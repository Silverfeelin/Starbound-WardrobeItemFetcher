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
        public static JObject Convert(JObject wearable, WearableType type, string assetPath, string fileName)
        {
            JObject newWearable = new JObject();

            newWearable["path"] = Path.GetDirectoryName(assetPath).Replace("\\", "/") + "/";
            newWearable["fileName"] = fileName;
            newWearable["category"] = Path.GetExtension(fileName).ToLowerInvariant().Replace(".", "");

            newWearable["name"] = wearable["itemName"];
            newWearable["shortdescription"] = wearable.GetValue("shortdescription", StringComparison.OrdinalIgnoreCase); // In case shortDescription is valid and used.
            newWearable["icon"] = wearable["inventoryIcon"];
            newWearable["maleFrames"] = wearable["maleFrames"];
            newWearable["femaleFrames"] = wearable["femaleFrames"];
            newWearable["mask"] = wearable["mask"];
            JToken rarity = wearable["rarity"];
            if (rarity != null && rarity.Type == JTokenType.String)
            {
                newWearable["rarity"] = rarity.Value<string>().ToLowerInvariant();
            }
            else
            {
                newWearable["rarity"] = "common";
            }
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

            return newWearable;
        }
    }
}
