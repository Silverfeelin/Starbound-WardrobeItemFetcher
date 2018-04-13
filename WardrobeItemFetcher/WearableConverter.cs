using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace WardrobeItemFetcher
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
        public static JObject Convert(JObject wearable, WearableType type)
        {
            JObject newWearable = new JObject();
            
            // TODO: Remove/rename parameters. Wardrobe doesn't use a bunch of parameters so this is mostly to conserve data.

            return newWearable;
        }
    }
}
