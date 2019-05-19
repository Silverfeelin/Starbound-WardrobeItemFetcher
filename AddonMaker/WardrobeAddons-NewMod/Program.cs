using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Settings = WardrobeAddons_NewMod.Properties.Settings;

namespace WardrobeAddons_NewMod
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure
            if (args.ToList().Contains("--configure"))
            {
                Configure();
                return;
            }

            // Validate configured paths
            ValidateSettings();

            if (args.Length == 1 && Directory.Exists(args[0]))
                UpdateMod(args[0]);
            else
                NewMod();
        }
        
        private static void NewMod()
        {
            Console.WriteLine("- Creating a new Wardrobe add-on.");

            // Add-ons directory
            var path = Settings.Default.Addons;

            // Mod name
            Console.WriteLine("Mod name (identifier)?");
            var name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name)) return;
            var nameCode = name.Substring(0, 1).ToLowerInvariant() + name.Substring(1).Replace(" ", "");

            var modPath = Path.Combine(path, $"Wardrobe-{name}");
            var wardrobePath = Path.Combine(modPath, "wardrobe");
            var dumpFile = Path.Combine(wardrobePath, $"{nameCode}.json");

            // Check add-on folder
            if (Directory.Exists(modPath))
            {
                WaitAndExit("Mod already exists.");
                return;
            }

            // Mod visual name
            Console.WriteLine("Visual name?");
            var visual = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(visual)) return;

            // Author
            Console.WriteLine("Author?");
            var author = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(author)) return;

            // Version
            Console.WriteLine("Version? (skip = 1.0)");
            var version = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(version)) version = "1.0";

            // Create directories
            Directory.CreateDirectory(modPath);
            Directory.CreateDirectory(Path.Combine(modPath, "wardrobe"));

            // Create metadata
            var metadata = new JObject
            {
                ["author"] = author,
                ["description"] = $"Wardrobe Add-on for {visual}.\n\nRequires both the Wardrobe and {visual}.",
                ["friendlyName"] = $"Wardrobe - {visual}",
                ["includes"] = new JArray { "Wardrobe" },
                ["name"] = $"Wardrobe-{name}",
                ["tags"] = "Cheats and God Items|User Interface|Armor and Clothes",
                ["version"] = version
            };
            File.WriteAllText(Path.Combine(modPath, "_metadata"), metadata.ToString(Formatting.Indented));

            // Create patch
            var patch = new JArray
            {
                new JObject
                {
                    ["op"] = "add",
                    ["path"] = "/mod/-",
                    ["value"] = $"{nameCode}.json"
                }
            };
            File.WriteAllText(Path.Combine(wardrobePath, "wardrobe.config.patch"), patch.ToString(Formatting.Indented));
            
            // Call WardrobeItemFetcher
            Console.WriteLine("Assets path?");
            var assetPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(assetPath)) return;
            Fetch(path, assetPath, dumpFile);

            // Move to mods folder
            Copier.CopyDirectory(modPath, Path.Combine(Settings.Default.Mods, $"Wardrobe-{name}"));

            WaitAndExit("Finished.");
        }

        private static void UpdateMod(string addonPath)
        {
            Console.WriteLine("- Updating a Wardrobe add-on.");

            Console.WriteLine("Asset path?");
            var assetPath = Console.ReadLine();

            // Find wardrobe file
            var wardrobePath = Path.Combine(addonPath, "wardrobe");
            var files = Directory.GetFiles(wardrobePath);

            string file;
            try
            {
                file = files.SingleOrDefault(f => f.EndsWith(".json"));
            }
            catch (InvalidOperationException)
            {
                WaitAndExit("Can't update file; multiple .json files found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(file))
            {
                WaitAndExit("Can't update file; no JSON found.");
                return;
            }

            // Call WardrobeItemFetcher
            Fetch(Settings.Default.Addons, assetPath, file);

            WaitAndExit("Finished.");
        }

        // Use the WardrobeItemFetcher.
        private static void Fetch(string addonsPath, string assetPath, string outFile)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = $"dotnet",
                    Arguments = $"\"{addonsPath}\\WardrobeItemFetcher\\WardrobeItemFetcher.dll\" -i \"{assetPath}\" -o \"{outFile}\" --overwrite",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            // Handle output
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);

            // Wait for WardrobeItemFetcher.
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        // Validates if the mod folder and add-on folder exist.
        private static void ValidateSettings()
        {
            if (!Directory.Exists(Settings.Default.Mods))
                WaitAndExit("Mod directory not found. Please launch with argument --configure.");

            if (!Directory.Exists(Settings.Default.Addons))
                WaitAndExit("Addons directory not found. Please launch with argument --configure.");

            var itemFetcher = Path.Combine(Settings.Default.Addons, "WardrobeItemFetcher\\WardrobeItemFetcher.dll");
            if (!File.Exists(itemFetcher))
                WaitAndExit($"Item fetcher not found at {itemFetcher}.\n" +
                    "Please download and unpack the item fetcher to the above directory.\n" +
                    "https://github.com/Silverfeelin/Starbound-WardrobeItemFetcher/releases");
        }

        // Shows a message and waits for a key press before exiting.
        private static void WaitAndExit(string message, params object[] args)
        {
            Console.WriteLine(message, args);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        // Configure settings.
        private static void Configure()
        {
            Console.WriteLine("Add-ons path?");
            Settings.Default.Addons = Console.ReadLine();

            Console.WriteLine("Mod path?");
            Settings.Default.Mods = Console.ReadLine();

            Settings.Default.Save();
        }
    }
}
