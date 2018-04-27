using System.Collections.Generic;

namespace WardrobeItemFetcher.Fetcher
{
    public delegate void ItemFound(string path, string content);

    public interface IFetcher
    {
        ISet<string> Extensions { get; set; }

        event ItemFound OnItemFound;

        void Fetch(string path);
    }
}
