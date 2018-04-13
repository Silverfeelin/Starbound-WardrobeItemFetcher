using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace WardrobeItemFetcher
{
    public static class PatchBuilder
    {
        /// <summary>
        /// Creates a patch operation to add an object to an array.
        /// </summary>
        /// <param name="path">Path to array.</param>
        /// <param name="token">Object or array to add.</param>
        public static JObject AddOperation(string path, JToken token)
        {
            if (!IsValidPath(path))
            {
                throw new ArgumentException($"Invalid patch path '{path}'.");
            }
            
            JObject op = new JObject();
            op["op"] = "add";
            op["path"] = path;
            op["value"] = token;

            return op;
        }
        
        /// <summary>
        /// Returns a value indicating whether the path points to an object key.
        /// To patch arrays, use "" (root).
        /// <para>
        /// Criteria: Begins with /, ends with a non-numeric key.
        /// </para>
        /// <para>
        /// Valid: "/", "/foo", "/foo/bar", "/foo/_bar", "/foo1", "/1foo", "/-", "/2".
        /// </para>
        /// <para>
        /// Invalid: "", "/foo/", "foo".
        /// </para>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsValidPath(string path)
        {
            Regex regex = new Regex("^(?:\\/[^ \\/]+)*\\/?$");
            Match m = regex.Match(path);
            return m.Success;
        }
    }
}
