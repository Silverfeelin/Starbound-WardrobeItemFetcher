using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WardrobeItemFetcher
{
    public delegate void EntryHandler(ZipArchiveEntry entry);

    public class ArchiveFetcher
    {
        /// <summary>
        /// Gets or sets the file extensions to find. If null or empty, all files are considered valid.
        /// Only lowercase entries without a dot should be used in this set (i.e. "chest").
        /// </summary>
        public ISet<string> Extensions { get; set; }

        /// <summary>
        /// When fetching entries from the archive, this event is invoked for every entry matching the <see cref="Extensions"/>.
        /// </summary>
        public event EntryHandler OnEntryFound;

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
            if (archive == null)
            {
                throw new ArgumentNullException("The ZipArchive can not be null.");
            }

            if (OnEntryFound == null)
            {
                throw new ArgumentNullException("The OnEntryFound event must have at least one subscriber.");
            }

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (Extensions == null || Extensions.Count == 0 ||
                    Extensions.Contains(Path.GetExtension(entry.FullName).Replace(".", "").ToLowerInvariant()))
                {
                    OnEntryFound?.Invoke(entry);
                }
            }
        }
    }
}
