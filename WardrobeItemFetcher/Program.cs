using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WardrobeItemFetcher.Fetcher;

namespace WardrobeItemFetcher
{
    class Options
    {
        [Option('i', "input", Required = true, HelpText = "Directory or file to search in. Asset directories, `.pak` and `.zip` files are supported (but not zipped pak files).")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file.")]
        public string OutputPath { get; set; }

        [Option('p', "patch", HelpText = "Create a patch (wearables.json.patch) instead of a normal file (wearables.json).")]
        public bool Patch { get; set; }

        [Option("overwrite", HelpText = "Overwrites the output file if it already exists.")]
        public bool OverwriteFile { get; set; }

        [Option('f', "format", HelpText = "Format JSON.")]
        public bool Format { get; set; }

        [Option('n', "names", HelpText = "Only store item names")]
        public bool Names { get; set; }
    }

    class Program
    {
        public static readonly HashSet<string> EXTENSIONS = new HashSet<string>()
        {
            "head", "chest", "legs", "back"
        };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => Run(opts));
        }

        enum ContentType
        {
            Directory,
            Pak,
            Zip
        };

        static void Run(Options options)
        {
            string input = options.InputPath;
            ContentType contentType = input.EndsWith(".pak") ? ContentType.Pak : input.EndsWith(".zip") ? ContentType.Zip : ContentType.Directory;

            // Confirm input/output
            ConfirmInput(input, contentType);
            ConfirmOutput(options.OutputPath, options.OverwriteFile);
            
            // Log information
            Console.WriteLine("= WardrobeItemFetcher =");
            Console.WriteLine("https://github.com/Silverfeelin/Starbound-WardrobeItemFetcher");
            Console.WriteLine();
            Console.WriteLine("- Options");
            Console.WriteLine("  Source: {0}", options.InputPath);
            Console.WriteLine("  Output: {0}", options.OutputPath);
            Console.WriteLine("    Type: {0}", options.Patch ? "JSON Array" : "JSON Object");
            Console.WriteLine(" Content: {0}", options.Names ? "Item names" : "Wearable configs");
            Console.WriteLine();
            Console.WriteLine("- Output");
            Console.WriteLine("Please wait while the application finds wearables...");

            // Create wearables JSON.
            JToken output;
            try
            {
                // Get fetcher
                IFetcher fetcher;
                switch (contentType)
                {
                    default:
                    case ContentType.Directory:
                        fetcher = new DirectoryFetcher();
                        break;
                    case ContentType.Pak:
                        fetcher = new PakFetcher();
                        break;
                    case ContentType.Zip:
                        fetcher = new ArchiveFetcher();
                        break;
                }

                fetcher.Extensions = EXTENSIONS;

                // Fetch
                bool namesOnly = options.Names;
                output = options.Patch ? (JToken)WardrobeItemFetcher.CreatePatch(fetcher, options.InputPath, namesOnly) : WardrobeItemFetcher.CreateObject(fetcher, options.InputPath, namesOnly);
            }
            catch (Exception exc)
            {
                Exit($"Error: Failed to create JSON:{Environment.NewLine}  {exc.Message}", 1);
                return;
            }

            // Write to disk.
            try
            {
                File.WriteAllText(options.OutputPath, output.ToString(options.Format ? Formatting.Indented : Formatting.None));
            }
            catch (Exception exc)
            {
                Exit($"Error: Failed to write contents to '{options.OutputPath}':{Environment.NewLine}  {exc.Message}", 1);
                return;
            }

            Console.WriteLine("Done!");
        }

        /// <summary>
        /// Confirms the input path, and errors with exit code 1 if it's invalid.
        /// </summary>
        /// <param name="path">Input path.</param>
        /// <param name="type">Type of input.</param>
        private static void ConfirmInput(string path, ContentType type)
        {
            // Confirm input
            switch (type)
            {
                case ContentType.Directory:
                    if (!Directory.Exists(path))
                    {
                        Exit($"Error: Asset directory '{path}' does not exist.", 1);
                        return;
                    }
                    break;
                case ContentType.Pak:
                    if (!File.Exists(path))
                    {
                        Exit($"Error: Pak file '{path}' does not exist.", 1);
                        return;
                    }
                    break;
                case ContentType.Zip:
                    if (!File.Exists(path))
                    {
                        Exit($"Error: Zip file '{path}' does not exist.", 1);
                        return;
                    }
                    break;
            }
        }

        /// <summary>
        /// Confirms the output file path, and errors with exit code 1 if it's invalid.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="canOverwrite">Can the file be overwritten?</param>
        private static void ConfirmOutput(string path, bool canOverwrite)
        {
            FileInfo outputFile = new FileInfo(path);

            // Confirm output
            if (!outputFile.Directory.Exists)
            {
                Exit($"Error: Output directory '{outputFile.Directory.FullName}' does not exist.", 1);
                return;
            }

            if (outputFile.Exists && !canOverwrite)
            {
                Exit($"Error: Output file '{outputFile.FullName}' already exists and flag --overwrite not set.", 1);
                return;
            }

            if (Directory.Exists(outputFile.FullName))
            {
                Exit($"Error: Output file '{outputFile.FullName}' is a directory.", 1);
                return;
            }

        }

        /// <summary>
        /// Exits with an error code after writing a message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="exitCode">Exit code.</param>
        private static void Exit(string message, int exitCode = 0)
        {
            Console.WriteLine(message);
            Environment.Exit(exitCode);
        }
    }
}
