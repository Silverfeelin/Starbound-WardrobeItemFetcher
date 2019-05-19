using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WardrobeItemFetcher.Fetcher
{
    public class DirectoryFetcher : IFetcher
    {
        /// <summary>
        /// Gets or sets the file extensions to find. If null or empty, all files are considered valid.
        /// Only lowercase entries without a dot should be used in this set (i.e. "chest").
        /// </summary>
        public ISet<string> Extensions { get; set; }

        /// <summary>
        /// Invoked when calling <see cref="Fetch"/> for every file found matching any extension in <see cref="Extensions"/>.
        /// </summary>
        public event ItemFoundHandler OnItemFound;

        /// <summary>
        /// Returns the (asset path) for a file.
        /// <para>
        ///     RelativePath("C:\\", "C:\\Starbound\\someFile.txt") => "/Starbound/someFile.txt".
        /// </para>
        /// </summary>
        /// <param name="assetFolder">Full path to asset folder (i.e. the scanned mod folder).</param>
        /// <param name="filePath">Full path to file.</param>
        /// <returns>Asset path to file.</returns>
        public static string AssetPath(string assetFolder, string filePath)
        {
            return "/" + Path.GetRelativePath(assetFolder, filePath).Replace("\\", "/");
        }
        
        /// <summary>
        /// Fetches all files in the given directory matching any extension in <see cref="Extensions"/>.
        /// </summary>
        /// <param name="baseDirectory">Directory to search in.</param>
        /// <param name="recursive">Search in subdirectories.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void Fetch(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException("Directory '" + directoryInfo.FullName + "' does not exist.");
            }

            ScanDirectory(directoryInfo, directoryInfo, true);
        }

        /// <summary>
        /// Gets all files in <paramref name="directory"/> matching any extension in <see cref="Extensions"/>.
        /// </summary>
        /// <param name="directory">Directory to search in.</param>
        /// <param name="recursive">Search in subdirectories.</param>
        private void ScanDirectory(DirectoryInfo baseDirectory, DirectoryInfo directory, bool recursive)
        {
            FileInfo[] files = directory.GetFiles("*.*");

            IEnumerable<FileInfo> filteredFiles =
                Extensions != null && Extensions.Count > 0
                ? files.Where(f => Extensions.Contains(f.Extension.Replace(".", "").ToLowerInvariant()))
                : files.AsEnumerable();

            foreach (FileInfo file in filteredFiles)
            {
                string s = File.ReadAllText(file.FullName);
                string path = AssetPath(baseDirectory.FullName, file.FullName);

                OnItemFound?.Invoke(path, s);
            }

            if (recursive)
            {
                foreach (DirectoryInfo dir in directory.GetDirectories())
                {
                    ScanDirectory(baseDirectory, dir, true);
                }
            }
        }
    }
}
