using System.Collections.Generic;
using System.IO;
using System.Text;
using WardrobeItemFetcher.Pak;

namespace WardrobeItemFetcher.Fetcher
{
    public class PakFetcher : IFetcher
    {
        /// <summary>
        /// Gets or sets the file extensions to find. If null or empty, all files are considered valid.
        /// Only lowercase entries without a dot should be used in this set (i.e. "chest").
        /// </summary>
        public ISet<string> Extensions { get; set; }

        public event ItemFound OnItemFound;
        
        public void Fetch(string pakPath)
        {
            using (FileStream fs = File.Open(pakPath, FileMode.Open))
            using (BinaryReader binaryReader = new BinaryReader(fs))
            {
                PakReader reader = new PakReader();
                PakData pak = reader.Read(binaryReader);

                foreach (PakItem item in pak.Items)
                {
                    string ext = Path.GetExtension(item.Path);
                    if (Extensions == null || Extensions.Count == 0 ||
                        ext.Length > 0 && Extensions.Contains(ext.Substring(1).ToLowerInvariant()))
                    {
                        byte[] data = PakReader.ReadItem(binaryReader, item);
                        string s = Encoding.UTF8.GetString(data);

                        OnItemFound?.Invoke(item.Path, s);
                    }
                }
            }   
        }
    }
}
