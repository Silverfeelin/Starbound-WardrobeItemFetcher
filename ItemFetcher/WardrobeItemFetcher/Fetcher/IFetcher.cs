using System.Collections.Generic;

namespace WardrobeItemFetcher.Fetcher
{
    /// <summary>
    /// Handler to parse an item found by the fetcher.
    /// </summary>
    /// <param name="path">Asset path to item (includes file name).</param>
    /// <param name="content">Asset content (text).</param>
    public delegate void ItemFoundHandler(string path, string content);

    /// <summary>
    /// Interface for item fetchers.
    /// Implementations should be able to fetch text files from a file or directory path and invoke <see cref="OnItemFound"/> to notify listeners.
    /// </summary>
    public interface IFetcher
    {
        /// <summary>
        /// Lowercase set of file extensions to fetch.
        /// Extensions should not contain a dot, and only text files should be fetched.
        /// </summary>
        ISet<string> Extensions { get; set; }

        /// <summary>
        /// Invoked when an item is found. Item asset path (including file name) and contents are passed as strings.
        /// </summary>
        event ItemFoundHandler OnItemFound;

        /// <summary>
        /// Fetches all items at the given file or directory path.
        /// </summary>
        /// <param name="path">File or directory path (depending on implementation).</param>
        void Fetch(string path);
    }
}
