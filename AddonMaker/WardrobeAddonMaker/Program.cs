using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WardrobeAddonMaker
{
    public class Program
    {
        private static string _applicationPath;
        private static string _modsPath;
        private static string _addonsPath;
        private static string _itemFetcherPath;

        public static void Main(string[] args)
        {
            _applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            var config = new Configuration(Path.Combine(_applicationPath, "config.json"));

            if (args.Any(a => a == "--configure"))
            {
                var success = Configure(config);
                Console.WriteLine(success ? "Configuration saved." : "Configuration not saved or failed to save.");
                return;
            }

            if (!ValidateSettings(config))
            {
                Console.WriteLine("Please run with --configure.");
                return;
            }

            _modsPath = config.Data.Value<string>("ModPath");
            _addonsPath = config.Data.Value<string>("AddonPath");
            _itemFetcherPath = config.Data.Value<string>("ItemFetcherPath");

            if (args.Length == 0)
                NewMod();
            else
                UpdateMod(args[0]);
        }

        private static void NewMod()
        {
            Console.WriteLine("= Wardrobe Add-on Maker - New =");

            // Identifier
            Console.Write("Mod identifier: ");
            var identifier = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                Console.WriteLine("No value provided.");
                return;
            }

            var identifierCode = identifier.Substring(0, 1).ToLowerInvariant() +
                                 identifier.Substring(1).Replace(" ", string.Empty);
            var addonPath = Path.Combine(_addonsPath, $"Wardrobe-{identifier}");
            var wardrobePath = Path.Combine(addonPath, "wardrobe");
            var itemFile = Path.Combine(wardrobePath, $"{identifierCode}.json");

            if (Directory.Exists(addonPath))
            {
                Console.WriteLine("Add-on already exists! Switching to update mode...");
                UpdateMod(addonPath);
                return;
            }

            // Mod name
            Console.Write("Mod name: ");
            var name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("No value provided.");
                return;
            };

            // Author
            Console.Write("Author: ");
            var author = Console.ReadLine() ?? "";

            // Version
            Console.Write("Version: ");
            var version = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(version))
                version = "1.0";

            // Create directories
            Directory.CreateDirectory(addonPath);
            Directory.CreateDirectory(wardrobePath);

            // Create metadata
            var metadata = new JObject
            {
                ["author"] = author,
                ["description"] = $"Wardrobe Add-on for {name}.\n\nRequires both the Wardrobe and {name}.",
                ["friendlyName"] = $"Wardrobe - {name}",
                ["includes"] = new JArray {"Wardrobe"},
                ["name"] = $"Wardrobe-{identifier}",
                ["tags"] = "Cheats and God Items|User Interface|Armor and Clothes",
                ["version"] = version
            };
            File.WriteAllText(Path.Combine(addonPath, "_metadata"), metadata.ToString(Formatting.Indented));

            // Create patch
            var patch = new JArray
            {
                new JObject
                {
                    ["op"] = "add",
                    ["path"] = "/mod/-",
                    ["value"] = $"{identifierCode}.json"
                }
            };
            File.WriteAllText(Path.Combine(wardrobePath, "wardrobe.config.patch"), patch.ToString(Formatting.Indented));

            // Call WardrobeItemFetcher
            Console.Write("Assets path: ");
            var assetPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Console.WriteLine("No value provided.");
                return;
            }
            if (!Directory.Exists(assetPath))
            {
                Console.WriteLine("Directory does not exist.");
                return;
            }
            Fetch(assetPath, itemFile);

            // Copy to mods folder
            if (Directory.Exists(_modsPath))
                CopyHelper.CopyDirectory(addonPath, Path.Combine(_modsPath, $"Wardrobe-{identifier}"));
            
            Console.WriteLine("Done!");
        }

        private static void UpdateMod(string addonPath)
        {
            Console.WriteLine("= Wardrobe Add-on Maker - Update =");
            
            Console.Write("Assets path: ");
            var assetPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Console.WriteLine("No value provided.");
                return;
            }
            if (!Directory.Exists(assetPath))
            {
                Console.WriteLine("Directory does not exist.");
                return;
            }

            var wardrobePath = Path.Combine(addonPath, "wardrobe");
            var files = Directory.GetFiles(wardrobePath);

            string file;
            try
            {
                file = files.SingleOrDefault(f => f.EndsWith(".json"));
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Can't update file; multiple files found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(file))
            {
                Console.WriteLine("Can't update file; no file found.");
                return;
            }

            // Call WardrobeItemFetcher
            Fetch(assetPath, file);

            // Copy to mods folder
            if (Directory.Exists(_modsPath))
                CopyHelper.CopyDirectory(addonPath, Path.Combine(_modsPath, Path.GetFileName(addonPath)));

            Console.WriteLine("Done!");
        }

        // Use the WardrobeItemFetcher.
        private static void Fetch(string assetPath, string outFile)
        {
            var itemFetcher = Path.Combine(_itemFetcherPath, "WardrobeItemFetcher.dll");

            var process = new Process
            {
                StartInfo =
                {
                    FileName = $"dotnet",
                    Arguments = $"\"{itemFetcher}\" -i \"{assetPath}\" -o \"{outFile}\" --overwrite",
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


        private static bool ValidateSettings(Configuration config)
        {
            var data = config.Data;
            if (!data.ContainsKey("AddonPath"))
            {
                Console.WriteLine("- Application not configured.");
                return false;
            }

            var valid = true;
            var addonPath = data.Value<string>("AddonPath");
            
            if (!Directory.Exists(addonPath))
            {
                Console.WriteLine("- Add-on directory does not exist:");
                Console.WriteLine(addonPath);
                valid = false;
            }

            // Item fetcher
            var itemFetcherPath = data.Value<string>("ItemFetcherPath");
            var itemFetcherFile = Path.Combine(itemFetcherPath, "WardrobeItemFetcher.dll");
            if (!Directory.Exists(itemFetcherPath))
            {
                Console.WriteLine("- Wardrobe Item fetcher directory does not exist:");
                Console.WriteLine(itemFetcherPath);
                valid = false;
            }
            else if (!File.Exists(itemFetcherFile))
            {
                Console.WriteLine("- WardrobeItemFetcher.dll not found:");
                Console.WriteLine(itemFetcherFile);
                valid = false;
            }
            
            return valid;
        }

        private static bool Configure(Configuration config)
        {
            var data = config.Data;
            Console.WriteLine("Please enter the following details to set up the add-on maker.");

            // Mod path
            Console.WriteLine("- Starbound mod directory:");
            var modPath = Console.ReadLine();
            data["ModPath"] = modPath;
            if (!Directory.Exists(modPath))
                Console.WriteLine("Directory not found. Add-ons will not be moved to the mods folder.");

            // Add-on path.
            while (true)
            {
                Console.WriteLine("- Add-on directory:");
                var addonPath = Console.ReadLine();
                if (addonPath == string.Empty) return false;
                if (Directory.Exists(addonPath))
                {
                    data["AddonPath"] = addonPath;
                    break;
                }
                Console.WriteLine("Directory not found.");
            }

            // Item fetcher
            while (true)
            {
                Console.WriteLine("- Wardrobe Item Fetcher directory:");
                var itemFetcherPath = Console.ReadLine();
                if (itemFetcherPath == string.Empty) return false;
                if (Directory.Exists(itemFetcherPath))
                {
                    data["ItemFetcherPath"] = itemFetcherPath;
                    break;
                }
                Console.WriteLine("Directory not found.");
            }

            return config.Save();
        }
    }
}
