using System.IO;

namespace WardrobeAddonMaker
{
    public static class CopyHelper
    {
        // https://stackoverflow.com/a/3822913/8523745 by tboswell
        public static void CopyDirectory(string path, string target)
        {
            foreach (var dirPath in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(path, target));

            foreach (var newPath in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(path, target), true);
        }
    }
}
