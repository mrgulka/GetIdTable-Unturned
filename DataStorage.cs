using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;

namespace TestProject
{
    public class DataStorage<T>(string dir, string fileName) where T : class
    {
        private string DataPath { get; } = Path.Combine(dir, fileName);

        public void Save(T obj)
        {
            var objData = JsonConvert.SerializeObject(obj, (Newtonsoft.Json.Formatting)Formatting.Indented);

            using var stream = new StreamWriter(DataPath, false);
            stream.Write(objData);
        }
        public T? Read()
        {
            if (!File.Exists(DataPath))
            {
                return null;
            }

            string dataText;
            using (var stream = File.OpenText(DataPath))
            {
                dataText = stream.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<T>(dataText);
        }
    }

    public class ToJson
    {
        public static void Save<T>(T obj, string path)
        {
            var objData = JsonConvert.SerializeObject(obj, (Newtonsoft.Json.Formatting)Formatting.Indented);

            //var path = System.Reflection.Assembly.GetEntryAssembly().Location;
            var general = Program.DirectoryPath;
            general = general.Split('\\')[^1];

            if (!path.EndsWith('\\'))
            {
                path += "\\";
            }

            var fullpath = path + $"{general}.json";
            using var stream = new StreamWriter(fullpath, false);
            stream.Write(objData);
        }
    }
}
