using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace WardrobeItemFetcher
{
    class Options
    {
        [Option('i', "input", Required = true, HelpText = "Asset directory to search in.")]
        public string Directory { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file.")]
        public string OutputFile { get; set; }
        
        [Option('p', "patch", HelpText = "Create a patch (wearables.json.patch) instead of a normal file (wearables.json).")]
        public bool Patch { get; set; }

        [Option("overwrite", HelpText = "Overwrites the output file if it already exists.")]
        public bool OverwriteFile { get; set; }

        [Option('f', "format", HelpText = "Format JSON.")]
        public bool Format { get; set; }
    }

    class Program
    {
        static Options options;
        static DirectoryInfo rootDirectory;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => Run(opts));
        }

        static void Run(Options options)
        {
            Program.options = options;

            rootDirectory = new DirectoryInfo(options.Directory);
            FileInfo outputFile = new FileInfo(options.OutputFile);

            // Check paths.
            if (!rootDirectory.Exists)
            {
                Exit($"Error: Asset directory '{rootDirectory.FullName}' does not exist.", 1);
                return;
            }
            
            if (!outputFile.Directory.Exists)
            {
                Exit($"Error: Output directory '{outputFile.Directory.FullName}' does not exist.", 1);
                return;
            }
            
            if (outputFile.Exists && !options.OverwriteFile)
            {
                Exit($"Error: Output file '{outputFile.FullName}' already exists and flag --overwrite not set.", 1);
                return;
            }

            if (Directory.Exists(outputFile.FullName))
            {
                Exit($"Error: Output file '{outputFile.FullName}' is a directory.", 1);
                return;
            }

            Console.WriteLine("= WardrobeItemFetcher =");
            Console.WriteLine("https://github.com/Silverfeelin/Starbound-WardrobeItemFetcher");
            Console.WriteLine();
            Console.WriteLine("- Options");
            Console.WriteLine("Asset Directory: {0}", rootDirectory.FullName);
            Console.WriteLine("    Output File: {0}", outputFile.FullName);
            Console.WriteLine("      File Type: {0}", options.Patch ? "JSON Patch" : "JSON");
            Console.WriteLine();
            Console.WriteLine("- Output");
            Console.WriteLine("Please wait while the application finds valid wearables...");

            // Create wearables JSON.
            JToken output;
            try
            {
                output = options.Patch ? (JToken)WardrobeItemFetcher.CreatePatch(rootDirectory) : WardrobeItemFetcher.CreateObject(rootDirectory);
            }
            catch (Exception exc)
            {
                Exit($"Error: Failed to create JSON:{Environment.NewLine}  {exc.Message}", 1);
                return;
            }

            // Write to disk.
            try
            {
                File.WriteAllText(outputFile.FullName, output.ToString(options.Format ? Formatting.Indented : Formatting.None));
            }
            catch (Exception exc)
            {
                Exit($"Error: Failed to write contents to '{outputFile.FullName}':{Environment.NewLine}  {exc.Message}", 1);
                return;
            }

            Console.WriteLine("Done!");
        }
        
        private static void Exit(string message, int exitCode = 0)
        {
            Console.WriteLine(message);
            Environment.Exit(exitCode);
        }
    }
}
