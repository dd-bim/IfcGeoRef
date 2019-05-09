using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Serilog;

namespace IfcGeoRefChecker.IO
{
    public class JsonOutput
    {
        public void JsonOutputFile(Appl.GeoRefChecker checkObj, string file)
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

        public void JsonOutputDialog(Appl.GeoRefChecker checkObj, string filePath, string fileName)
        {
            var jsonObj = JsonConvert.SerializeObject(checkObj, Formatting.Indented);

            try
            {
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();

                saveFileDialog1.InitialDirectory = filePath;        //Pfad, der zunächst angeboten wird
                saveFileDialog1.DefaultExt = "json";
                saveFileDialog1.Filter = "json files (*.json)|*.json";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.Title = "Save json file with building perimeter (WKTRep-Attribute)";
                saveFileDialog1.RestoreDirectory = true;
                saveFileDialog1.FileName = fileName + "_WKT.json";
                saveFileDialog1.ShowDialog();

                var text = saveFileDialog1.FileName;

                using(StreamWriter sw = new StreamWriter(text))
                    sw.WriteLine(jsonObj);
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