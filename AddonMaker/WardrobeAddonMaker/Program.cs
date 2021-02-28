using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WardrobeAddonMaker
{
    public class Program
    {
        public class Options
        {
            [Option( "input", Required = false, HelpText = "Asset path to scan.")]
            public string Input { get; set; }

            [Option( "identifier", Required = false, HelpText = "Mod identifier (i.e. FrackinUniverse).")]
            public string Identifier { get; set; }

            [Option("name", Required = false, HelpText = "Mod name (i.e. Frackin' Universe).")]
            public string Name { get; set; }

            [Option("author", Required = false, HelpText = "Mod author (i.e. sayter).")]
            public string Author { get; set; }

            [Option("version", Required = false, HelpText = "Mod version (i.e. 5.6.3131).")]
            public string Version { get; set; }

            [Option("configure", Required = false, HelpText = "Configure necessary parameters for the tool.")]
            public string Configure { get; set; }
        }

        private static Configuration _config;
        private static string _applicationPath;
        private static string _modsPath;
        private static string _addonsPath;
        private static string _itemFetcherPath;

        public static void Main(string[] args)
        {
            _applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            _config = new Configuration(Path.Combine(_applicationPath, "config.json"));

            if (args.Any(a => a == "--configure"))
            {
                var success = Configure(_config);
                Console.WriteLine(success ? "Configuration saved." : "Configuration not saved or failed to save.");
                return;
            }

            if (!ValidateSettings(_config))
            {
                Console.WriteLine("Please run with --configure.");
                return;
            }

            _modsPath = _config.Data.Value<string>("ModPath");
            _addonsPath = _config.Data.Value<string>("AddonPath");
            _itemFetcherPath = _config.Data.Value<string>("ItemFetcherPath");


            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    var path = Path.Combine(_addonsPath, $"Wardrobe-{o.Identifier}");
                    if (Directory.Exists(path))
                        UpdateMod(o);
                    else
                        NewMod(o);
                });
        }

        private static void NewMod(Options o)
        {
            Console.WriteLine("= Wardrobe Add-on Maker - New =");

            // Identifier
            if (string.IsNullOrWhiteSpace(o.Identifier))
            {
                Console.Write("Mod identifier: ");
                o.Identifier = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(o.Identifier))
                {
                    Console.WriteLine("No value provided.");
                    return;
                }
            }
            var identifierCode = o.Identifier.Substring(0, 1).ToLowerInvariant() +
                                 o.Identifier.Substring(1).Replace(" ", string.Empty);
            var addonPath = Path.Combine(_addonsPath, $"Wardrobe-{o.Identifier}");
            var wardrobePath = Path.Combine(addonPath, "wardrobe");
            var itemFile = Path.Combine(wardrobePath, $"{identifierCode}.json");

            if (Directory.Exists(addonPath))
            {
                Console.WriteLine("Add-on already exists! Switching to update mode...");
                UpdateMod(o);
                return;
            }

            // Mod name
            if (string.IsNullOrWhiteSpace(o.Name))
            {
                Console.Write("Mod name: ");
                o.Name = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(o.Name))
                {
                    Console.WriteLine("No value provided.");
                    return;
                };
            }

            // Author
            if (string.IsNullOrWhiteSpace(o.Author))
            {
                Console.Write("Author: ");
                o.Author = Console.ReadLine() ?? "";
            }

            // Version
            if (string.IsNullOrWhiteSpace(o.Version))
            {
                Console.Write("Version: ");
                o.Version = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(o.Version))
                    o.Version = "1.0";
            }

            // Create directories
            Directory.CreateDirectory(addonPath);
            Directory.CreateDirectory(wardrobePath);

            // Create metadata
            var metadata = new JObject
            {
                ["author"] = o.Author,
                ["description"] = $"Wardrobe Add-on for {o.Name}.\n\nRequires both the Wardrobe and {o.Name}.",
                ["friendlyName"] = $"Wardrobe - {o.Name}",
                ["includes"] = new JArray {"Wardrobe"},
                ["name"] = $"Wardrobe-{o.Identifier}",
                ["tags"] = "Cheats and God Items|User Interface|Armor and Clothes",
                ["version"] = o.Version
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

            if (string.IsNullOrWhiteSpace(o.Input) || !Directory.Exists(o.Input))
            {
                Console.Write("Assets path: ");
                o.Input = Console.ReadLine();
                o.Input = o.Input?.Replace("\"", string.Empty);

                if (string.IsNullOrWhiteSpace(o.Input))
                {
                    Console.WriteLine("No value provided.");
                    return;
                }
            }

            if (!Directory.Exists(o.Input) && !File.Exists(o.Input))
            {
                Console.WriteLine("Asset directory or file does not exist.");
                return;
            }

            // Call WardrobeItemFetcher
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Fetch(o.Input, itemFile);
            Console.ResetColor();

            // Copy to mods folder
            if (Directory.Exists(_modsPath))
                CopyHelper.CopyDirectory(addonPath, Path.Combine(_modsPath, $"Wardrobe-{o.Identifier}"));
            
            Console.WriteLine("Done!");
        }

        private static void UpdateMod(Options o)
        {
            Console.WriteLine("= Wardrobe Add-on Maker - Update =");

            if (string.IsNullOrWhiteSpace(o.Input) || !Directory.Exists(o.Input))
            {
                Console.Write("Assets path: ");
                o.Input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(o.Input))
                {
                    Console.WriteLine("No value provided.");
                    return;
                }
            }

            if (!Directory.Exists(o.Input) && !File.Exists(o.Input))
            {
                Console.WriteLine("Asset directory or file does not exist.");
                return;
            }

            var addonPath = Path.Combine(_addonsPath, $"Wardrobe-{o.Identifier}");
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
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Fetch(o.Input, file);
            Console.ResetColor();

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
