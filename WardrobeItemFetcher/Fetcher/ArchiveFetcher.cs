using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace WardrobeItemFetcher.Fetcher
{
    public class ArchiveFetcher : IFetcher
    {
        /// <summary>
        /// Gets or sets the file extensions to find. If null or empty, all files are considered valid.
        /// Only lowercase entries without a dot should be used in this set (i.e. "chest").
        /// </summary>
        public ISet<string> Extensions { get; set; }
        
        /// <summary>
        /// Invoked when calling <see cref="Fetch"/> for every zip entry found matching any extension in <see cref="Extensions"/>.
        /// </summary>
        public event ItemFoundHandler OnItemFound;

        /// <summary>
        /// Returns the (asset path) for a file.
        /// <para>
        ///     RelativePath("/SomeMod/", "/SomeMod/someFile.txt") => "/someFile.txt".
        /// </para>
        /// </summary>
        /// <param name="assetFolder">Full path to asset folder (i.e. the folder containing a metadata file).</param>
        /// <param name="filePath">Full path to file.</param>
        /// <returns>Asset path to file.</returns>
        public static string AssetPath(string assetFolder, string filePath)
        {
            return "/" + Path.GetRelativePath(assetFolder, filePath).Replace("\\", "/");
        }

        /// <summary>
        /// Fetches all files in the archive matching any extension in <see cref="Extensions"/>.
        /// </summary>
        /// <param name="archivePath">Path to zip archive.</param>
        public void Fetch(string archivePath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                Fetch(archive);
            }   
        }

        /// <summary>
        /// Fetches all files in the archive matching any extension in <see cref="Extensions"/>.
        /// </summary>
        /// <param name="archive">Zip archive.</param>
        public void Fetch(ZipArchive archive)
        {
            // Get metadata to determine asset root
            ZipArchiveEntry metadata = archive.Entries.Where(e =>
            {
                var name = e.Name.ToLowerInvariant();
                return name == "_metadata" || name == ".metadata";
            }).FirstOrDefault();
            string root = metadata != null ? Path.GetDirectoryName(metadata.FullName) : "/";
            if (string.IsNullOrWhiteSpace(root)) root = "/";

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (Extensions == null || Extensions.Count == 0 ||
                    Extensions.Contains(Path.GetExtension(entry.FullName).Replace(".", "").ToLowerInvariant()))
                {
                    string s = ReadEntry(entry);
                    string path = AssetPath(root, entry.FullName);

                    OnItemFound?.Invoke(path, s);
                }
            }
        }
        
        /// <summary>
        /// Reads the zip archive entry as a text file.
        /// </summary>
        /// <param name="entry">Archive entry to read.</param>
        /// <returns>Text from file.</returns>
        private string ReadEntry(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
