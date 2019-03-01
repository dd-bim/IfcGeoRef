using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Serilog;

namespace IfcGeoRefChecker.IO
{
    public class JsonOutput
    {
        public JsonOutput(Appl.GeoRefChecker checkObj, string file/*, string direc*/)
        {
            var jsonObj = JsonConvert.SerializeObject(checkObj, Formatting.Indented);

            using(var writeJson = File.CreateText((/*direc + "\\" + */file + ".json")))
            {
                try
                {
                    writeJson.WriteLine(jsonObj);

                    Log.Information("JSON-file successfully exported.");
                }

                catch(Exception ex)
                {
                    var str = $"Error occured while writing JSON-file. \r\n Message: {ex.Message}";

                    Log.Error(str);
                    MessageBox.Show(str);
                }
            }
        }
    }
}