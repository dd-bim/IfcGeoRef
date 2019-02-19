using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using IfcGeoRefChecker.Appl;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.IO
{
    public class JsonOutput
    {
        public JsonOutput(Appl.GeoRefChecker checkObj, string file/*, string direc*/)
        {
            var jsonObj= JsonConvert.SerializeObject(checkObj, Formatting.Indented);

            using(var writeJson = File.CreateText((/*direc + "\\" + */file + ".json")))
            {
                try
                {
                    writeJson.WriteLine(jsonObj);
                }

                catch(Exception ex)
                {
                    MessageBox.Show($"Error occured while writing JSON-file. \r\n Message: {ex.Message}");
                }
            }
        }
    }
}