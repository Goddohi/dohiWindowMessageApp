using Newtonsoft.Json;
using System.Collections.Generic;

namespace WalkieDohi.Util
{
    public static class JsonUtil
    {
        public static string Serialize<T>(T obj, bool indented = false)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, settings);
        }

        public static T Deserialize<T>(string json)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}
