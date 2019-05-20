using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WardrobeAddonMaker
{
    public class Configuration
    {
        public JObject Data { get; }

        private readonly string _path;

        public Configuration(string path)
        {
            _path = path;
            Data = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }
        
        public bool Save()
        {
            try
            {
                File.WriteAllText(_path, Data.ToString(Formatting.Indented));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
