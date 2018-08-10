using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using IfcGeoRefChecker.Appl;
using Newtonsoft.Json;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.IO
{
    public class JsonOutput
    {
        public string GlobalID { get; set; }
        public string IFCSchema { get; set; }
        public string TimeCreation { get; set; }
        public string TimeCheck { get; set; }

        public List<Level10> LoGeoRef10 { get; set; } = new List<Level10>();

        public List<Level20> LoGeoRef20 { get; set; } = new List<Level20>();

        public List<Level30> LoGeoRef30 { get; set; } = new List<Level30>();

        public Level40 LoGeoRef40 { get; set; }

        public Level50 LoGeoRef50 { get; set; }

        public void WriteJSONfile(IfcStore model, string file)
        {
            using(var writeJson = File.CreateText((@".\results\GeoRef_" + file + ".json")))
            {
                try
                {
                    var proj = model.Instances.FirstOrDefault<IIfcProject>();
                    this.GlobalID = proj.GlobalId.ToString();

                    this.IFCSchema = model.IfcSchemaVersion.ToString();
                    this.TimeCreation = model.Header.TimeStamp;
                    this.TimeCheck = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);      //UTC timestamp

                    this.LoGeoRef40 = new Level40(model);
                    this.LoGeoRef50 = new Level50(model);

                    string jsonobj = JsonConvert.SerializeObject(this, Formatting.Indented);

                    writeJson.WriteLine(jsonobj);
                    this.LoGeoRef10.Clear();
                    this.LoGeoRef20.Clear();
                    this.LoGeoRef30.Clear();
                }

                catch(Exception ex)
                {
                    MessageBox.Show($"Error occured while writing JSON-file. \r\n Message: {ex.Message} + \r\n Stack: {ex.StackTrace}");
                }
            }
        }

        public void GetGeoRefElements10(Level10 georef10)
        {
            this.LoGeoRef10.Add(georef10);
        }

        public void GetGeoRefElements20(Level20 georef20)
        {
            this.LoGeoRef20.Add(georef20);
        }

        public void GetGeoRefElements30(Level30 georef30)
        {
            this.LoGeoRef30.Add(georef30);
        }
    }
}