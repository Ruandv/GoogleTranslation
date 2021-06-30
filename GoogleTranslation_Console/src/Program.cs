using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Web;

namespace GoogleTranslation_Console
{
    class Program
    {
        // https://cloud.google.com/translate/docs/languages
        /*
         Afrikaans	af
         German	    de
         English	en
         French	    fr
         Italian	it
         Urdu	    ur
        */

        static string apiKey = Environment.GetEnvironmentVariable("googleTranslateApiKey");
        static Dictionary<string, string> myData = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            MyArgs record = new MyArgs();
            var myArgs = new Dictionary<string, string>();
            if (args.Length != 4)
            {
                Console.WriteLine("You need to supply the following arguments : ");
                Console.WriteLine("Examples : ");
                Console.WriteLine("[Multi language translations] GoogleTranslation_Console.exe TargetLanguage=de;af TargetFileName=de-DE;af-AF CurrentFileLocation=C:\\temp\\en-us.json OutputFilePath=C:\\temp\\ ");
                Console.WriteLine("[Single language translations] GoogleTranslation_Console.exe TargetLanguage=af TargetFileName=af-AF CurrentFileLocation=C:\\temp\\en-us.json OutputFilePath=C:\\temp\\ ");
                Console.ReadKey();
                return;
            }

            foreach (string s in args)
            {
                myArgs.Add(s.Split("=")[0], s.Split("=")[1]);
            }

            PropertyInfo[] properties = typeof(MyArgs).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var value = myArgs[property.Name];
                property.SetValue(record, value);
            }

            string f = File.ReadAllText(record.CurrentFileLocation);
            JToken d = JsonConvert.DeserializeObject<JToken>(f);
            int index = 0;
            foreach (string lang in record.TargetLanguage.Split(';'))
            {
                myData.Clear();
                doWork(d, lang, apiKey);
                var betterDictionary = DotNotationToDictionary(myData);

                var json = JsonConvert.SerializeObject(betterDictionary);
                WriteToFile(json, record.TargetFileName.Split(";")[index]);
                index++;
            }
        }

        private static void WriteToFile(string json, string targetFile)
        {
            if (File.Exists(string.Format("C:\\temp\\{0}.json", targetFile)))
                File.Delete(string.Format("C:\\temp\\{0}.json", targetFile));
            using (StreamWriter sw = new StreamWriter(string.Format("C:\\temp\\{0}.json", targetFile)))
            {
                sw.WriteLine(json);
            }
            Console.WriteLine(json);
        }

        private static void doWork(JToken data, string target, string apiKey)
        {
            if (data.HasValues)
            {
                foreach (var x in data.Children())
                {
                    doWork(x, target, apiKey);
                }
            }
            else
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://translation.googleapis.com/language/translate/v2");
                var qdata = "?q=" + HttpUtility.UrlEncode(data.ToString()) + "&target=" + HttpUtility.UrlEncode(target) + "&key=" + apiKey;
                var httpResponse = httpClient.GetAsync(qdata).Result;
                httpResponse.EnsureSuccessStatusCode();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contentStream = httpResponse.Content.ReadAsStreamAsync().Result;

                    using var streamReader = new StreamReader(contentStream);
                    using var jsonReader = new JsonTextReader(streamReader);

                    JsonSerializer serializer = new JsonSerializer();

                    try
                    {
                        var res = serializer.Deserialize<dynamic>(jsonReader);
                        var translation = HttpUtility.UrlDecode(res["data"]["translations"][0]["translatedText"].ToString());
                        myData.Add(data.Path, translation);
                        Console.WriteLine(data.Path + " >> " + data.ToString() + " >> " + translation);
                    }
                    catch (JsonReaderException)
                    {
                        Console.WriteLine("Invalid JSON.");
                    }
                }
                httpClient.Dispose();
            }
        }
        public static Dictionary<string, object> DotNotationToDictionary(Dictionary<string, string> dotNotation)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            foreach (var dotObject in dotNotation)
            {
                var hierarcy = dotObject.Key.Split('.');

                Dictionary<string, object> bottom = dictionary;

                for (int i = 0; i < hierarcy.Length; i++)
                {
                    var key = hierarcy[i];

                    if (i == hierarcy.Length - 1)
                    {
                        bottom.Add(key, dotObject.Value);
                    }
                    else
                    {
                        if (!bottom.ContainsKey(key))
                            bottom.Add(key, new Dictionary<string, object>());

                        bottom = (Dictionary<string, object>)bottom[key];
                    }
                }
            }

            return dictionary;
        }
    }
    public class MyArgs
    {
        public string TargetLanguage { get; set; }
        public string TargetFileName { get; set; }
        public string CurrentFileLocation { get; set; }
        public string OutputFilePath { get; set; }
    }

}
