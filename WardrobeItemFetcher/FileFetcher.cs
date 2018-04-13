using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WardrobeItemFetcher
{
    /// <summary>
    /// File callback. Can be used to handle files found by the <see cref="FileFetcher"/>.
    /// </summary>
    /// <param name="file">File information for the found file.</param>
    public delegate void FileHandler(FileInfo file);

    public class FileFetcher
    {
        /// <summary>
        /// Gets or sets the file extensions to find. If null or empty, all files are considered valid.
        /// Only lowercase entries without a dot should be used in this set (i.e. "chest").
        /// </summary>
        public ISet<string> Extensions { get; set; }

        /// <summary>
        /// When fetching files, this event is invoked for each found file matching the <see cref="Extensions"/>.
        /// </summary>
        public event FileHandler OnFileFound;

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
        public void Fetch(DirectoryInfo baseDirectory, bool recursive)
        {
            if (!baseDirectory.Exists)
            {
                throw new DirectoryNotFoundException("Directory '" + baseDirectory.FullName + "' does not exist.");
            }

            if (OnFileFound == null)
            {
                throw new ArgumentNullException("The OnFileFound event must have at least one subscriber.");
            }

            ScanDirectory(baseDirectory, recursive);
        }

        /// <summary>
        /// Gets all files in <paramref name="directory"/> matching any extension in <see cref="Extensions"/>.
        /// </summary>
        /// <param name="directory">Directory to search in.</param>
        /// <param name="recursive">Search in subdirectories.</param>
        private void ScanDirectory(DirectoryInfo directory, bool recursive)
        {
            FileInfo[] files = directory.GetFiles("*.*");

            IEnumerable<FileInfo> filteredFiles =
                Extensions != null && Extensions.Count > 0
                ? files.Where(f => Extensions.Contains(f.Extension.Replace(".", "").ToLowerInvariant()))
                : files.AsEnumerable();

            foreach (FileInfo file in filteredFiles)
            {
                OnFileFound?.Invoke(file);
            }

            if (recursive)
            {
                foreach (DirectoryInfo dir in directory.GetDirectories())
                {
                    ScanDirectory(dir, true);
                }
            }
        }
    }
}
